using LibraryManagement.API.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LibraryManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Librarian")]
    public class DashboardController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/dashboard/stats (Unchanged)
        [HttpGet("stats")]
        public async Task<IActionResult> GetStats()
        {
            var totalBooks = await _context.Books.SumAsync(b => b.Quantity);
            var totalUsers = await _context.Users.CountAsync();
            var booksOnLoan = await _context.Loans.CountAsync(l => l.ReturnDate == null);
            var overdueBooks = await _context.Loans.CountAsync(l => l.ReturnDate == null && l.DueDate < DateTime.UtcNow);
            return Ok(new { totalBooks, totalUsers, booksOnLoan, overdueBooks });
        }

        // --- REPORTING ENDPOINTS RESTORED TO ORIGINAL STATE ---
        [HttpGet("most-popular-books")]
        public async Task<IActionResult> GetMostPopularBooks()
        {
            var report = await _context.Loans
                .Include(l => l.Book)
                .Where(l => l.Book != null)
                .GroupBy(l => new { l.Book.Title, l.Book.Author })
                .Select(g => new
                {
                    g.Key.Title,
                    g.Key.Author,
                    LoanCount = g.Count()
                })
                .OrderByDescending(r => r.LoanCount)
                .Take(10)
                .ToListAsync();
            return Ok(report);
        }

        [HttpGet("user-activity")]
        public async Task<IActionResult> GetUserActivity()
        {
            var report = await _context.Users
                .Include(u => u.Loans)
                .Select(u => new
                {
                    u.UserName,
                    u.Email,
                    BooksBorrowed = u.Loans.Count()
                })
                .OrderByDescending(r => r.BooksBorrowed)
                .Take(10)
                .ToListAsync();
            return Ok(report);
        }

        [HttpGet("fines-collected")]
        public async Task<IActionResult> GetFinesCollected()
        {
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);
            var totalFines = await _context.Loans
                .Where(l => l.ReturnDate != null && l.ReturnDate >= thirtyDaysAgo)
                .SumAsync(l => l.FineAmount);
            return Ok(new { Period = "Last 30 Days", TotalFines = totalFines });
        }

        [HttpGet("inventory-summary")]
        public async Task<IActionResult> GetInventorySummary()
        {
            var totalUniqueBooks = await _context.Books.CountAsync();
            var totalCopies = await _context.Books.SumAsync(b => b.Quantity);
            var booksOnLoan = await _context.Loans.CountAsync(l => l.ReturnDate == null);
            return Ok(new
            {
                TotalUniqueTitles = totalUniqueBooks,
                TotalCopiesInLibrary = totalCopies,
                CopiesCurrentlyOnLoan = booksOnLoan,
                AvailableCopiesOnShelves = totalCopies - booksOnLoan
            });
        }
    }
}