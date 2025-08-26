using LibraryManagement.API.Data;
using LibraryManagement.API.Models;
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
    [Authorize] // All actions require an authenticated user
    public class WishlistController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public WishlistController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/wishlist
        // Gets the current user's complete wishlist.
        [HttpGet]
        public async Task<IActionResult> GetWishlist()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var wishlistBooks = await _context.WishlistItems
                .Where(wi => wi.ApplicationUserId == userId)
                .Include(wi => wi.Book) // Include the full book details
                .Select(wi => wi.Book)
                .ToListAsync();

            return Ok(wishlistBooks);
        }

        // GET: api/wishlist/ids
        // A lightweight endpoint to get just the IDs of books on the user's wishlist.
        // This is very useful for the UI to quickly check if a book is on the list.
        [HttpGet("ids")]
        public async Task<IActionResult> GetWishlistIds()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var wishlistBookIds = await _context.WishlistItems
                .Where(wi => wi.ApplicationUserId == userId)
                .Select(wi => wi.BookId)
                .ToListAsync();

            return Ok(wishlistBookIds);
        }


        // POST: api/wishlist/{bookId}
        // Adds a book to the current user's wishlist.
        [HttpPost("{bookId:int}")]
        public async Task<IActionResult> AddToWishlist(int bookId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Check if the book exists
            var bookExists = await _context.Books.AnyAsync(b => b.Id == bookId);
            if (!bookExists)
            {
                return NotFound(new { Message = "Book not found." });
            }

            // Check if it's already on the wishlist (our composite key also prevents this)
            var alreadyExists = await _context.WishlistItems.AnyAsync(wi =>
                wi.ApplicationUserId == userId && wi.BookId == bookId);
            if (alreadyExists)
            {
                return Ok(new { Message = "Book is already on your wishlist." }); // Not an error
            }

            var wishlistItem = new WishlistItem
            {
                ApplicationUserId = userId,
                BookId = bookId
            };

            _context.WishlistItems.Add(wishlistItem);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Book added to your wishlist." });
        }

        // DELETE: api/wishlist/{bookId}
        // Removes a book from the current user's wishlist.
        [HttpDelete("{bookId:int}")]
        public async Task<IActionResult> RemoveFromWishlist(int bookId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var wishlistItem = await _context.WishlistItems.FirstOrDefaultAsync(wi =>
                wi.ApplicationUserId == userId && wi.BookId == bookId);

            if (wishlistItem == null)
            {
                return NotFound(new { Message = "This book is not on your wishlist." });
            }

            _context.WishlistItems.Remove(wishlistItem);
            await _context.SaveChangesAsync();

            return Ok(new { Message = "Book removed from your wishlist." });
        }
    }
}