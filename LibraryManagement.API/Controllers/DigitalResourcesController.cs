using LibraryManagement.API.Data;
using LibraryManagement.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.ComponentModel.DataAnnotations;
using System.IO; // <--- THIS IS THE CRITICAL MISSING 'USING' STATEMENT
using System.Linq;
using System.Threading.Tasks;

namespace LibraryManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class DigitalResourcesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly string _uploadsFolderPath;

        public DigitalResourcesController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            // Build the absolute path to our "PrivateUploads" folder.
            _uploadsFolderPath = Path.Combine(env.ContentRootPath, "PrivateUploads");

            // Create the directory if it doesn't exist to prevent errors.
            if (!Directory.Exists(_uploadsFolderPath))
            {
                Directory.CreateDirectory(_uploadsFolderPath);
            }
        }

        [HttpPost("upload")]
        [Authorize(Roles = "Librarian")]
        public async Task<IActionResult> UploadResource([FromForm] UploadResourceModel model)
        {
            if (model.File == null || model.File.Length == 0)
                return BadRequest(new { Message = "No file was selected for upload." });

            var storedFileName = $"{Guid.NewGuid()}{Path.GetExtension(model.File.FileName)}";
            var filePath = Path.Combine(_uploadsFolderPath, storedFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await model.File.CopyToAsync(stream);
            }

            var resource = new DigitalResource
            {
                Title = model.Title,
                Author = model.Author,
                Subject = model.Subject,
                OriginalFileName = model.File.FileName,
                StoredFileName = storedFileName
            };

            _context.DigitalResources.Add(resource);
            await _context.SaveChangesAsync();

            return Ok(resource);
        }

        [HttpGet]
        [Authorize(Roles = "Teacher, Librarian")]
        public async Task<IActionResult> GetResources()
        {
            var resources = await _context.DigitalResources
                .OrderBy(r => r.Title)
                .Select(r => new { r.Id, r.Title, r.Author, r.Subject, r.OriginalFileName })
                .ToListAsync();

            return Ok(resources);
        }

        [HttpGet("download/{id}")]
        [Authorize(Roles = "Teacher")]
        public async Task<IActionResult> DownloadResource(int id)
        {
            var resource = await _context.DigitalResources.FindAsync(id);
            if (resource == null) return NotFound();

            var filePath = Path.Combine(_uploadsFolderPath, resource.StoredFileName);
            if (!System.IO.File.Exists(filePath))
                return NotFound(new { Message = "File not found on server." });

            var memory = new MemoryStream();
            using (var stream = new FileStream(filePath, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;

            return File(memory, "application/pdf", resource.OriginalFileName);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Librarian")]
        public async Task<IActionResult> DeleteResource(int id)
        {
            var resource = await _context.DigitalResources.FindAsync(id);
            if (resource == null) return NotFound();

            var filePath = Path.Combine(_uploadsFolderPath, resource.StoredFileName);
            if (System.IO.File.Exists(filePath))
            {
                System.IO.File.Delete(filePath);
            }

            _context.DigitalResources.Remove(resource);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    // A helper model for the multipart/form-data upload
    public class UploadResourceModel
    {
        [Required] public string Title { get; set; }
        public string Author { get; set; }
        public string Subject { get; set; }
        [Required] public IFormFile File { get; set; }
    }
}