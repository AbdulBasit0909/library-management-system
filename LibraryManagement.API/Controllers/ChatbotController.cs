using LibraryManagement.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace LibraryManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Only logged-in users can use the chatbot
    public class ChatbotController : ControllerBase
    {
        private readonly AIRecommendationService _aiService;

        public ChatbotController(AIRecommendationService aiService)
        {
            _aiService = aiService;
        }

        // POST: api/chatbot/query
        [HttpPost("query")]
        public async Task<IActionResult> PostQuery([FromBody] ChatQueryDto queryDto)
        {
            if (string.IsNullOrWhiteSpace(queryDto.Query))
            {
                return BadRequest(new { Message = "Query cannot be empty." });
            }

            // Get the response from our AI service
            var aiResponse = await _aiService.GetChatbotResponseAsync(queryDto.Query);

            // Return the response in a simple JSON object
            return Ok(new { Response = aiResponse });
        }
    }

    // A simple DTO for the request body
    public class ChatQueryDto
    {
        [Required]
        public string Query { get; set; }
    }
}