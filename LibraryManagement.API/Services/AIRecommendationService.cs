using LibraryManagement.API.Data;
using LibraryManagement.API.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace LibraryManagement.API.Services
{
    public class AIRecommendationService
    {
        private readonly HttpClient _httpClient;
        private readonly ApplicationDbContext _dbContext;
        private readonly string _apiKey;

        public AIRecommendationService(HttpClient httpClient, ApplicationDbContext dbContext, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _dbContext = dbContext;
            _apiKey = configuration["OpenAI:ApiKey"];
        }

        // --- THIS METHOD IS NOW FULLY CORRECTED FOR OPENROUTER ---
        public async Task<List<Book>> GetRecommendationsAsync(Book sourceBook)
        {
            var apiUrl = "https://openrouter.ai/api/v1/chat/completions";

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            _httpClient.DefaultRequestHeaders.Remove("HTTP-Referer");
            _httpClient.DefaultRequestHeaders.Remove("X-Title");
            _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://localhost:7019");
            _httpClient.DefaultRequestHeaders.Add("X-Title", "Library Management System");

            var prompt = $"Based on the book '{sourceBook.Title}', suggest 3 similar book titles. Do not suggest the same book or its direct sequels. Return ONLY a raw JSON array of strings, like [\"Title 1\", \"Title 2\", \"Title 3\"]. Do not add any other text or markdown formatting.";

            // Use the correct OpenAI/OpenRouter payload structure
            var payload = new
            {
                model = "mistralai/mistral-7b-instruct", // A reliable free model
                messages = new[]
                {
                    new { role = "user", content = prompt }
                }
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync(apiUrl, payload);
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"--- RECOMMENDATION API ERROR: {errorBody} ---");
                    return await GetFallbackRecommendations(sourceBook);
                }

                var aiResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
                string rawText = aiResponse.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();

                List<string> recommendedTitles = JsonSerializer.Deserialize<List<string>>(rawText);
                if (recommendedTitles == null || !recommendedTitles.Any())
                {
                    return await GetFallbackRecommendations(sourceBook);
                }

                var foundBooks = await _dbContext.Books
                    .Where(b => recommendedTitles.Contains(b.Title) && b.Id != sourceBook.Id)
                    .Take(3)
                    .ToListAsync();

                return !foundBooks.Any() ? await GetFallbackRecommendations(sourceBook) : foundBooks;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--- RECOMMENDATION PARSING ERROR: {ex.Message} ---");
                return await GetFallbackRecommendations(sourceBook);
            }
        }

        private async Task<List<Book>> GetFallbackRecommendations(Book sourceBook)
        {
            var bookWithCategory = await _dbContext.Books
                .AsNoTracking()
                .FirstOrDefaultAsync(b => b.Id == sourceBook.Id);

            if (bookWithCategory?.CategoryId == null)
            {
                return new List<Book>();
            }

            return await _dbContext.Books
                .AsNoTracking()
                .Where(b => b.CategoryId == bookWithCategory.CategoryId && b.Id != sourceBook.Id)
                .Take(3)
                .ToListAsync();
        }

        // This chatbot method is already correct
        public async Task<string> GetChatbotResponseAsync(string userQuery)
        {
            var apiUrl = "https://openrouter.ai/api/v1/chat/completions";

            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            _httpClient.DefaultRequestHeaders.Remove("HTTP-Referer");
            _httpClient.DefaultRequestHeaders.Remove("X-Title");
            _httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "https://localhost:7019");
            _httpClient.DefaultRequestHeaders.Add("X-Title", "Library Management System");

            var prompt = $"You are a friendly librarian's assistant. A user asked: '{userQuery}'. Answer the question and suggest one or two relevant book titles in a link foam that user clicks it naviates to that book. You MUST wrap each book title in double asterisks, like **Book Title**. Keep your response concise.";

            var payload = new
            {
                model = "mistralai/mistral-7b-instruct",
                messages = new[]
                {
                    new { role = "system", content = prompt },
                    new { role = "user", content = userQuery }
                }
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync(apiUrl, payload);
                if (!response.IsSuccessStatusCode)
                {
                    var errorBody = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"--- CHATBOT API ERROR: {errorBody} ---");
                    return "I'm sorry, I'm having trouble connecting to my knowledge base right now. Please try again in a moment.";
                }

                var aiResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
                return aiResponse.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--- CHATBOT PARSING ERROR: {ex.Message} ---");
                return "I'm sorry, an unexpected error occurred. Please try again later.";
            }
        }
    }
}
