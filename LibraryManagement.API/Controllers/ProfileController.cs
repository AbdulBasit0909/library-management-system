using LibraryManagement.API.Data;
using LibraryManagement.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Security.Claims;
using System.Threading.Tasks;

namespace LibraryManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // All actions require a logged-in user
    public class ProfileController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly string _avatarsFolderPath;

        public ProfileController(UserManager<ApplicationUser> userManager, IWebHostEnvironment env)
        {
            _userManager = userManager;
            // Create a dedicated, secure subfolder for avatars
            _avatarsFolderPath = Path.Combine(env.ContentRootPath, "PrivateUploads", "avatars");

            if (!Directory.Exists(_avatarsFolderPath))
            {
                Directory.CreateDirectory(_avatarsFolderPath);
            }
        }

        // POST: api/profile/upload-picture
        [HttpPost("upload-picture")]
        public async Task<IActionResult> UploadProfilePicture(IFormFile file)
        {
            var user = await _userManager.FindByIdAsync(User.FindFirstValue(ClaimTypes.NameIdentifier));
            if (user == null) return Unauthorized();

            if (file == null || file.Length == 0)
                return BadRequest(new { Message = "No file was selected." });

            // Generate a unique filename based on the user's ID to prevent conflicts
            var fileExtension = Path.GetExtension(file.FileName);
            var storedFileName = $"{user.Id}{fileExtension}";
            var filePath = Path.Combine(_avatarsFolderPath, storedFileName);

            // Delete old profile picture if it exists
            if (!string.IsNullOrEmpty(user.ProfilePictureUrl))
            {
                var oldFilePath = Path.Combine(_avatarsFolderPath, Path.GetFileName(user.ProfilePictureUrl));
                if (System.IO.File.Exists(oldFilePath))
                {
                    System.IO.File.Delete(oldFilePath);
                }
            }

            // Save the new file
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Update the user's record in the database
            // We store a relative path that our GetPicture endpoint can use
            user.ProfilePictureUrl = $"api/profile/picture/{user.Id}";
            await _userManager.UpdateAsync(user);

            return Ok(new { FilePath = user.ProfilePictureUrl });
        }

        // GET: api/profile/picture/{userId}
        [HttpGet("picture/{userId}")]
        [AllowAnonymous] // Allow anyone to request a picture if they know the user's ID
        public async Task<IActionResult> GetPicture(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            // We find the filename from the URL, which is safer
            var fileName = $"{user.Id}{Path.GetExtension(user.ProfilePictureUrl ?? ".jpg")}";

            var filePath = Path.Combine(_avatarsFolderPath, fileName);

            if (user == null || string.IsNullOrEmpty(user.ProfilePictureUrl) || !System.IO.File.Exists(filePath))
            {
                // Optional: Return a default avatar image
                return NotFound();
            }

            var imageBytes = await System.IO.File.ReadAllBytesAsync(filePath);
            return File(imageBytes, "image/jpeg"); // Or appropriate content type
        }
    }
}