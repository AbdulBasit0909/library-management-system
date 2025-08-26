using LibraryManagement.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LibraryManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // All actions in this controller require a logged-in user
    public class NotificationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public NotificationsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/notifications
        // Gets the current user's most recent notifications (e.g., top 10)
        [HttpGet]
        public async Task<IActionResult> GetMyNotifications()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.DateCreated)
                .Take(10) // We don't want to fetch thousands, just the most recent
                .Select(n => new { n.Id, n.Message, n.IsRead, n.DateCreated, n.Url })
                .ToListAsync();

            return Ok(notifications);
        }

        // GET: api/notifications/unread-count
        // A lightweight endpoint to get just the count for the badge
        [HttpGet("unread-count")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var count = await _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead);
            return Ok(new { Count = count });
        }

        // POST: api/notifications/mark-all-as-read
        // Marks all of the user's unread notifications as read
        [HttpPost("mark-all-as-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var unreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            if (unreadNotifications.Any())
            {
                foreach (var notification in unreadNotifications)
                {
                    notification.IsRead = true;
                }
                await _context.SaveChangesAsync();
            }

            return Ok(new { Message = "All notifications marked as read." });
        }
    }
}