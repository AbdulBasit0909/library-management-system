using LibraryManagement.API.Data;
using LibraryManagement.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LibraryManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReviewsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ReviewsController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: api/reviews/{bookId}
        // [Public] Gets all reviews and the average rating for a specific book.
        [HttpGet("{bookId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetReviewsForBook(int bookId)
        {
            var reviews = await _context.Reviews
                .Where(r => r.BookId == bookId)
                .Include(r => r.User) // Include user to get their username
                .OrderByDescending(r => r.DatePosted)
                .Select(r => new
                {
                    r.Id,
                    r.Rating,
                    r.Comment,
                    r.DatePosted,
                    UserName = r.User.UserName
                })
                .ToListAsync();

            var averageRating = reviews.Any() ? reviews.Average(r => r.Rating) : 0;

            return Ok(new { AverageRating = averageRating, Reviews = reviews });
        }

        // POST: api/reviews
        // [Authenticated Users Only] Creates a new review for a book.
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateReview([FromBody] CreateReviewDto reviewDto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // --- BUSINESS RULE 1: Check if the user has actually borrowed and returned this book ---
            var hasBorrowed = await _context.Loans.AnyAsync(l =>
                l.ApplicationUserId == userId &&
                l.BookId == reviewDto.BookId &&
                l.ReturnDate != null);

            if (!hasBorrowed)
            {
                return StatusCode(403, new { Message = "You can only review books you have previously borrowed and returned." });
            }

            // --- BUSINESS RULE 2: Check if the user has already reviewed this book ---
            var alreadyReviewed = await _context.Reviews.AnyAsync(r =>
                r.ApplicationUserId == userId &&
                r.BookId == reviewDto.BookId);

            if (alreadyReviewed)
            {
                return BadRequest(new { Message = "You have already submitted a review for this book." });
            }

            var review = new Review
            {
                BookId = reviewDto.BookId,
                ApplicationUserId = userId,
                Rating = reviewDto.Rating,
                Comment = reviewDto.Comment,
                DatePosted = DateTime.UtcNow
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetReviewsForBook), new { bookId = review.BookId }, review);
        }
    }

    // A DTO for creating a new review
    public class CreateReviewDto
    {
        [Required]
        public int BookId { get; set; }

        [Required]
        [Range(1, 5)]
        public int Rating { get; set; }

        [StringLength(2000)]
        public string Comment { get; set; }
    }
}