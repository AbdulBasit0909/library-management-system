using LibraryManagement.API.Data;
using LibraryManagement.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using LibraryManagement.API.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Linq;
using System.Threading.Tasks;

namespace LibraryManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ReservationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHubContext<NotificationHub> _hubContext;

        public ReservationsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
        }

        // Helper method to keep our code clean and reusable
        private async Task CreateNotificationAsync(string userId, string message, string url)
        {
            var notification = new Notification
            {
                UserId = userId,
                Message = message,
                Url = url
            };
            _context.Notifications.Add(notification);
        }

        // POST: api/reservations/{bookId} (Student/Teacher create a request)
        [HttpPost("{bookId:int}")]
        [Authorize(Roles = "Student,Teacher")]
        public async Task<IActionResult> CreateReservation(int bookId)
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            var book = await _context.Books.FindAsync(bookId);

            if (user == null) return Unauthorized();
            if (book == null) return NotFound(new { Message = "Book not found." });

            var reservation = new Reservation
            {
                BookId = bookId,
                ApplicationUserId = user.Id,
                RequestDate = DateTime.UtcNow,
                Status = "Pending"
            };

            // 1. Add the reservation to the context
            _context.Reservations.Add(reservation);

            // 2. Prepare the persistent database notifications for all librarians
            var messageToLibrarian = $"New request for '{book.Title}' by '{user.UserName}'.";
            var librarianRole = await _context.Roles.FirstOrDefaultAsync(r => r.Name == "Librarian");
            if (librarianRole != null)
            {
                var librarians = await _context.UserRoles
                    .Where(ur => ur.RoleId == librarianRole.Id)
                    .Select(ur => ur.UserId)
                    .ToListAsync();

                foreach (var librarianId in librarians)
                {
                    await CreateNotificationAsync(librarianId, messageToLibrarian, "/librarian/reservations");
                }
            }

            // 3. Save both the new reservation and all notifications to the database
            await _context.SaveChangesAsync();

            // 4. Send the real-time (SignalR) notification to any active librarians
            await _hubContext.Clients.Group("LibrariansGroup")
                .SendAsync("ReceiveNotification", messageToLibrarian, "info");

            return Ok(new { Message = "Reservation request sent successfully." });
        }

        // GET: api/reservations/pending (Librarian views requests)
        [HttpGet("pending")]
        [Authorize(Roles = "Librarian")]
        public async Task<IActionResult> GetPendingReservations()
        {
            var pendingReservations = await _context.Reservations
                .Where(r => r.Status == "Pending")
                .Include(r => r.Book)
                .Include(r => r.ApplicationUser)
                .Select(r => new
                {
                    ReservationId = r.Id,
                    BookTitle = r.Book != null ? r.Book.Title : "Deleted Book",
                    UserName = r.ApplicationUser != null ? r.ApplicationUser.UserName : "Deleted User",
                    RequestDate = r.RequestDate
                })
                .ToListAsync();
            return Ok(pendingReservations);
        }

        // POST: api/reservations/approve/{reservationId} (Librarian approves)
        [HttpPost("approve/{reservationId:int}")]
        [Authorize(Roles = "Librarian")]
        public async Task<IActionResult> ApproveReservation(int reservationId)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Book)
                .Include(r => r.ApplicationUser)
                .FirstOrDefaultAsync(r => r.Id == reservationId);

            if (reservation == null || reservation.Book == null || reservation.ApplicationUser == null)
            {
                return NotFound(new { Message = "Reservation, book, or user not found." });
            }

            if (reservation.Book.Quantity <= 0)
            {
                return BadRequest(new { Message = "Book is out of stock." });
            }

            const int studentLoanDays = 3;
            const int teacherLoanDays = 5;
            var userRoles = await _userManager.GetRolesAsync(reservation.ApplicationUser);
            DateTime dueDate = userRoles.Contains("Teacher") ? DateTime.UtcNow.AddDays(teacherLoanDays) : DateTime.UtcNow.AddDays(studentLoanDays);

            reservation.Status = "Approved";
            var loan = new Loan
            {
                BookId = reservation.BookId,
                ApplicationUserId = reservation.ApplicationUserId,
                LoanDate = DateTime.UtcNow,
                DueDate = dueDate
            };
            _context.Loans.Add(loan);
            reservation.Book.Quantity--;

            var messageToUser = $"Your reservation for '{reservation.Book.Title}' has been approved!";

            // Create the persistent database notification
            await CreateNotificationAsync(reservation.ApplicationUserId, messageToUser, "/my-books");

            await _context.SaveChangesAsync();

            // Send the real-time SignalR notification
            await _hubContext.Clients.Group(reservation.ApplicationUser.UserName)
                .SendAsync("ReceiveNotification", messageToUser, "success");

            return Ok(new { Message = "Reservation approved and book issued." });
        }

        // POST: api/reservations/reject/{reservationId} (Librarian rejects)
        [HttpPost("reject/{reservationId:int}")]
        [Authorize(Roles = "Librarian")]
        public async Task<IActionResult> RejectReservation(int reservationId)
        {
            var reservation = await _context.Reservations
                .Include(r => r.Book)
                .Include(r => r.ApplicationUser)
                .FirstOrDefaultAsync(r => r.Id == reservationId);

            if (reservation == null || reservation.Book == null || reservation.ApplicationUser == null)
            {
                return NotFound(new { Message = "Reservation not found." });
            }

            _context.Reservations.Remove(reservation);

            var messageToUser = $"Your reservation for the book '{reservation.Book.Title}' has been rejected.";

            // Create the persistent database notification
            await CreateNotificationAsync(reservation.ApplicationUserId, messageToUser, "/catalog");

            await _context.SaveChangesAsync();

            // Send the real-time SignalR notification
            await _hubContext.Clients.Group(reservation.ApplicationUser.UserName)
                .SendAsync("ReceiveNotification", messageToUser, "warning");

            return Ok(new { Message = "Reservation rejected." });
        }
    }
}