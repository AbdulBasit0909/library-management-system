using LibraryManagement.API.Data;
using LibraryManagement.API.DTOs;
using LibraryManagement.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity; 
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace LibraryManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class LoansController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private const decimal FinePerDay = 0.25m;

        public LoansController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // POST: api/loans/issue
        [HttpPost("issue")]
        [Authorize(Roles = "Librarian")]
        public async Task<IActionResult> IssueBook([FromBody] IssueRequest model)
        {
            var book = await _context.Books.FindAsync(model.BookId);
            if (book == null || book.Quantity <= 0)
                return BadRequest(new { Message = "Book is not available for loan." });

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
                return NotFound(new { Message = "User not found." });


            const int studentLoanDays = 1;
            const int teacherLoanDays = 3;
            var userRoles = await _userManager.GetRolesAsync(user);
            DateTime dueDate;

            if (userRoles.Contains("Teacher"))
            {
                dueDate = DateTime.UtcNow.AddDays(teacherLoanDays);
            }
            else
            {
                dueDate = DateTime.UtcNow.AddDays(studentLoanDays);
            }
            // --- THIS IS THE CRUCIAL BACKEND FIX ---
            // We take the calculated due date and set its time to the very end of that day.
            // This ensures all books are due at 23:59:59 UTC, eliminating time-of-day bugs.
            dueDate = dueDate.Date.AddDays(1).AddTicks(-1);
            // --- END OF FIX ---

            var loan = new Loan
            {
                BookId = book.Id,
                ApplicationUserId = user.Id,
                LoanDate = DateTime.UtcNow,
                DueDate = dueDate, // Use the calculated due date
                FineAmount = 0
            };

            book.Quantity--;
            _context.Loans.Add(loan);
            await _context.SaveChangesAsync();

            var loanDto = new LoanDto
            {
                Id = loan.Id,
                BookTitle = book.Title,
                UserName = user.UserName,
                LoanDate = loan.LoanDate,
                DueDate = loan.DueDate,
                FineAmount = loan.FineAmount
            };
            return Ok(loanDto);
        }

        // POST: api/loans/return/{loanId:int}
        [HttpPost("return/{loanId:int}")]
        [Authorize(Roles = "Librarian")]
        public async Task<IActionResult> ReturnBook(int loanId)
        {
            var loan = await _context.Loans.Include(l => l.Book).FirstOrDefaultAsync(l => l.Id == loanId);
            if (loan == null)
                return NotFound(new { Message = "Loan record not found." });

            if (loan.ReturnDate.HasValue)
                return BadRequest(new { Message = "This book has already been returned." });

            var returnDate = DateTime.UtcNow;
            decimal fine = 0;

            // --- THIS IS THE CORRECTED LOGIC ---
            if (returnDate.Date > loan.DueDate.Date)
            {
                // Compare only the date part to get the accurate number of full days overdue.
                var daysOverdue = (int)(returnDate.Date - loan.DueDate.Date).TotalDays;

                // Ensure we don't accidentally get a negative number if something is wrong
                if (daysOverdue > 0)
                {
                    fine = daysOverdue * FinePerDay;
                }
            }
            // --- END OF FIX ---

            loan.ReturnDate = returnDate;
            loan.FineAmount = fine;

            if (loan.Book != null)
            {
                loan.Book.Quantity++;
            }

            await _context.SaveChangesAsync();

            var message = "Book returned successfully.";
            if (fine > 0)
            {
                message += $" A fine of ${fine:F2} has been applied.";
            }

            return Ok(new { Message = message });
        }

        // GET: api/loans/all
        [HttpGet("all")]
        [Authorize(Roles = "Librarian")]
        public async Task<ActionResult<IEnumerable<LoanDto>>> GetAllActiveLoans()
        {
            var loans = await _context.Loans
                .Where(l => l.ReturnDate == null)
                .Include(l => l.Book)
                .Include(l => l.ApplicationUser)
                .Select(l => new LoanDto
                {
                    Id = l.Id,
                    BookTitle = l.Book != null ? l.Book.Title : "Deleted Book",
                    UserName = l.ApplicationUser != null ? l.ApplicationUser.UserName : "Deleted User",
                    LoanDate = l.LoanDate,
                    DueDate = l.DueDate,
                    FineAmount = l.FineAmount
                })
                .ToListAsync();

            return Ok(loans);
        }

        // GET: api/loans/myloans
        [HttpGet("myloans")]
        public async Task<ActionResult<IEnumerable<LoanDto>>> GetMyLoans()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return Unauthorized();

            var loans = await _context.Loans
                .Where(l => l.ApplicationUser.UserName == username && l.ReturnDate == null)
                .Include(l => l.Book)
                .Select(l => new LoanDto
                {
                    Id = l.Id,
                    BookId = l.BookId,
                    BookTitle = l.Book != null ? l.Book.Title : "Deleted Book",
                    UserName = username,
                    LoanDate = l.LoanDate,
                    DueDate = l.DueDate,
                    FineAmount = l.FineAmount
                })
                .ToListAsync();

            return Ok(loans);
        }

        // GET: api/loans/myhistory
        [HttpGet("myhistory")]
        public async Task<ActionResult<IEnumerable<LoanDto>>> GetMyHistory()
        {
            // --- THIS IS THE FIX ---
            // We now use the secure and reliable User ID instead of the username.
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }

            var loans = await _context.Loans
                .Where(l => l.ApplicationUserId == userId && l.ReturnDate != null) // <-- The query now uses UserId
                .Include(l => l.Book)
                .OrderByDescending(l => l.ReturnDate)
                .Select(l => new LoanDto
                {
                    Id = l.Id,
                    BookId = l.BookId,
                    BookTitle = l.Book != null ? l.Book.Title : "Deleted Book",
                    UserName = l.ApplicationUser.UserName, // We can still get the username for display
                    LoanDate = l.LoanDate,
                    DueDate = l.DueDate,
                    FineAmount = l.FineAmount
                })
                .ToListAsync();

            return Ok(loans);
        }
        // --- NEW ENDPOINT 1: Get all loans with outstanding fines ---//
        [HttpGet("outstanding-fines")]
        [Authorize(Roles = "Librarian")]
        public async Task<ActionResult<IEnumerable<LoanDto>>> GetOutstandingFines()
        {
            var loansWithFines = await _context.Loans
                .Where(l => l.FineAmount > 0 && !l.FinePaid) // The core logic: fine exists but is not paid
                .Include(l => l.Book)
                .Include(l => l.ApplicationUser)
                .OrderByDescending(l => l.ReturnDate)
                .Select(l => new LoanDto
                {
                    Id = l.Id,
                    BookTitle = l.Book != null ? l.Book.Title : "Deleted Book",
                    UserName = l.ApplicationUser != null ? l.ApplicationUser.UserName : "Deleted User",
                    LoanDate = l.LoanDate,
                    DueDate = l.DueDate,
                    FineAmount = l.FineAmount
                })
                .ToListAsync();

            return Ok(loansWithFines);
        }

        // --- NEW ENDPOINT 2: Mark a fine as paid ---
        [HttpPost("payfine/{loanId:int}")]
        [Authorize(Roles = "Librarian")]
        public async Task<IActionResult> PayFine(int loanId)
        {
            var loan = await _context.Loans.FindAsync(loanId);

            if (loan == null)
            {
                return NotFound(new { Message = "Loan record not found." });
            }

            if (loan.FineAmount <= 0)
            {
                return BadRequest(new { Message = "This loan has no fine to be paid." });
            }

            if (loan.FinePaid)
            {
                return BadRequest(new { Message = "This fine has already been paid." });
            }

            loan.FinePaid = true;
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Fine marked as paid successfully." });
        }

        [HttpPost("renew/{loanId:int}")]
        [Authorize(Roles = "Student,Teacher")] // Only students and teachers can renew
        public async Task<IActionResult> RenewLoan(int loanId)
        {
            var currentUsername = User.Identity?.Name; // Get the username from the token
            if (string.IsNullOrEmpty(currentUsername))
            {
                return Unauthorized();
            }

            // Find the loan by its ID AND by the current user's username
            var loan = await _context.Loans
                .Include(l => l.ApplicationUser)
                .FirstOrDefaultAsync(l => l.Id == loanId && l.ApplicationUser.UserName == currentUsername);

            if (loan == null)
            {
                return NotFound(new { Message = "Loan record not found or you do not have permission to renew it." });
            }

            if (loan.ReturnDate != null)
            {
                return BadRequest(new { Message = "Cannot renew a book that has already been returned." });
            }

            // --- Business Rules ---
            const int maxRenewals = 2; // Business Rule: A book can only be renewed twice.
            if (loan.RenewalCount >= maxRenewals)
            {
                return BadRequest(new { Message = $"This book has already been renewed the maximum number of times ({maxRenewals})." });
            }

            // --- Renewal Logic ---
            const int studentRenewalDays = 7;
            const int teacherRenewalDays = 14;
            var userRoles = await _userManager.GetRolesAsync(loan.ApplicationUser);
            var renewalPeriod = userRoles.Contains("Teacher") ? teacherRenewalDays : studentRenewalDays;

            // Extend the due date from the original due date
            loan.DueDate = loan.DueDate.AddDays(renewalPeriod);
            loan.RenewalCount++;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Book renewed successfully!", NewDueDate = loan.DueDate });
        }


    }

    public class IssueRequest
    {
        [Required]
        public int BookId { get; set; }
        [Required]
        public string UserId { get; set; }
    }
}