using LibraryManagement.API.Data;
using LibraryManagement.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LibraryManagement.API.DTOs;

namespace LibraryManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BooksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BooksController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/Books (Public and Paginated)
        [HttpGet]
        [AllowAnonymous]
        public async Task<ActionResult<PagedResult<Book>>> GetBooks(
           [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] int? categoryId = null,
    [FromQuery] string? searchTerm = null) // <-- Make sure this parameter is here

        {
            // Start building the query from the database context
            var query = _context.Books.AsQueryable();

            // Apply Category Filter
            if (categoryId.HasValue && categoryId > 0)
            {
                query = query.Where(b => b.CategoryId == categoryId.Value);
            }

            // Apply Search Term Filter
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(b =>
                    b.Title.Contains(searchTerm) ||
                    b.Author.Contains(searchTerm)
                );
            }

            // Now, we can safely check and use the 'categoryId' parameter
            if (categoryId.HasValue && categoryId > 0)
            {
                query = query.Where(b => b.CategoryId == categoryId.Value);
            }

            // The rest of the method executes on the (potentially filtered) query
            var totalCount = await query.CountAsync();
            var books = await query
                .OrderBy(b => b.Title)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var result = new PagedResult<Book>
            {
                Items = books,
                TotalCount = totalCount,
                TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
                CurrentPage = pageNumber,
                PageSize = pageSize
            };

            return Ok(result);
        }

        // --- THIS IS THE NEW, CORRECTLY PLACED METHOD ---
        // GET: api/Books/all (Public, for search boxes)
        [HttpGet("all")]
        [AllowAnonymous]
        public async Task<ActionResult<IEnumerable<Book>>> GetAllBooks()
        {
            return await _context.Books.OrderBy(b => b.Title).ToListAsync();
        }
        // --- END OF NEW METHOD ---

        // GET: api/Books/5 (Public)
        [HttpGet("{id}")]
        [AllowAnonymous]
        public async Task<ActionResult<Book>> GetBook(int id)
        {
            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                return NotFound();
            }
            return book;
        }

        // POST: api/Books (Librarian Only) - NOW USES DTO
        [HttpPost]
        [Authorize(Roles = "Librarian")]
        public async Task<ActionResult<Book>> CreateBook(BookRequestDto bookDto)
        {
            // Manually map the DTO to the full Book entity
            var book = new Book
            {
                Title = bookDto.Title,
                Author = bookDto.Author,
                Description = bookDto.Description, // <-- Add this
                CoverImageUrl = bookDto.CoverImageUrl, // <-- Add this
                ISBN = bookDto.ISBN,
                PublishedDate = bookDto.PublishedDate,
                Quantity = bookDto.Quantity,
                CategoryId = bookDto.CategoryId
            };

            _context.Books.Add(book);
            await _context.SaveChangesAsync();

            // Return the full book object that was created
            return CreatedAtAction(nameof(GetBook), new { id = book.Id }, book);
        }

        // PUT: api/Books/5 (Librarian Only) - NOW USES DTO
        [HttpPut("{id}")]
        [Authorize(Roles = "Librarian")]
        public async Task<IActionResult> UpdateBook(int id, BookRequestDto bookDto)
        {
            var bookFromDb = await _context.Books.FindAsync(id);
            if (bookFromDb == null)
            {
                return NotFound();
            }

            // Manually map the properties from the DTO to the entity from the database
            bookFromDb.Title = bookDto.Title;
            bookFromDb.Author = bookDto.Author;
            bookFromDb.Description = bookDto.Description; // <-- Add this
            bookFromDb.CoverImageUrl = bookDto.CoverImageUrl; // <-- Add this
            bookFromDb.ISBN = bookDto.ISBN;
            bookFromDb.PublishedDate = bookDto.PublishedDate;
            bookFromDb.Quantity = bookDto.Quantity;
            bookFromDb.CategoryId = bookDto.CategoryId; // This now works correctly

            await _context.SaveChangesAsync();
            return NoContent();
        }
        // DELETE: api/Books/5 (Librarian Only)
        [HttpDelete("{id}")]
        [Authorize(Roles = "Librarian")]
        public async Task<IActionResult> DeleteBook(int id)
        {
            var hasActiveLoans = await _context.Loans.AnyAsync(l => l.BookId == id && l.ReturnDate == null);
            if (hasActiveLoans)
            {
                return BadRequest(new { Message = "Cannot delete this book because it is currently on loan." });
            }

            var book = await _context.Books.FindAsync(id);
            if (book == null)
            {
                return NotFound();
            }

            _context.Books.Remove(book);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }
}