using LibraryManagement.API.Data;
using LibraryManagement.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;

namespace LibraryManagement.API.Controllers
{
    [Route("api/recommendations")] // The route is now explicitly plural
    [ApiController]
    public class RecommendationController : ControllerBase
    {
        private readonly AIRecommendationService _aiService;
        private readonly ApplicationDbContext _context;

        public RecommendationController(AIRecommendationService aiService, ApplicationDbContext context)
        {
            _aiService = aiService;
            _context = context;
        }

        // GET: api/recommendations/{bookId}
        [HttpGet("{bookId:int}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetRecommendations(int bookId)
        {
            var sourceBook = await _context.Books
                .Include(b => b.Category)
                .FirstOrDefaultAsync(b => b.Id == bookId);

            if (sourceBook == null)
            {
                return NotFound();
            }

            var recommendations = await _aiService.GetRecommendationsAsync(sourceBook);

            return Ok(recommendations);
        }
    }
}