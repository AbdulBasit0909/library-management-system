using LibraryManagement.API.Data;
using LibraryManagement.API.DTOs;
using LibraryManagement.API.Models;
using LibraryManagement.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.ComponentModel.DataAnnotations;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Web; //  this for HttpUtility

namespace LibraryManagement.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ApplicationDbContext _context;
        private readonly IEmailSender _emailSender;
        public AuthController(
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ApplicationDbContext context,
            IEmailSender emailSender)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _httpContextAccessor = httpContextAccessor;
            _context = context;
            _emailSender = emailSender;
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] LoginModel model)
        {
            var user = await _userManager.FindByNameAsync(model.Username);
            if (user != null && await _userManager.CheckPasswordAsync(user, model.Password))
            {
                var userRoles = await _userManager.GetRolesAsync(user);
                var authClaims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

                         // --- THIS IS THE CRITICAL LINE ---
            // It adds the user's unique ID to the token.
            new Claim(ClaimTypes.NameIdentifier, user.Id)
                };
                foreach (var userRole in userRoles)
                {
                    authClaims.Add(new Claim(ClaimTypes.Role, userRole));
                }

                var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));
                var token = new JwtSecurityToken(
                    issuer: _configuration["JWT:ValidIssuer"],
                    audience: _configuration["JWT:ValidAudience"],
                    expires: DateTime.Now.AddHours(7),
                    claims: authClaims,
                    signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
                );

                var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

                // Store token in HttpContext for later use
                if (_httpContextAccessor.HttpContext != null)
                {
                    _httpContextAccessor.HttpContext.Items["JWTToken"] = tokenString;
                }

                return Ok(new
                {
                    token = tokenString,
                    expiration = token.ValidTo
                });
            }
            return Unauthorized();
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterModel model)
        {
            var userExists = await _userManager.FindByNameAsync(model.Username);
            if (userExists != null)
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Status = "Error", Message = "User already exists!" });

            ApplicationUser user = new()
            {
                Email = model.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = model.Username
            };
            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
            {
                var errors = result.Errors.Select(e => e.Description);
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Status = "Error", Message = $"User creation failed: {string.Join(", ", errors)}" });
            }
            if (!await _roleManager.RoleExistsAsync(model.Role))
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    new { Status = "Error", Message = "This role does not exist." });
            }
            await _userManager.AddToRoleAsync(user, model.Role);
            return Ok(new { Status = "Success", Message = "User created successfully!" });
        }

        // --- NEW ENDPOINT 1: FORGOT PASSWORD ---
        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // IMPORTANT: Do not reveal that the user does not exist.
                // This is a security measure to prevent email enumeration attacks.
                return Ok(new { Message = "If an account with this email exists, a password reset link has been sent." });
            }

            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = HttpUtility.UrlEncode(token);

            // IMPORTANT: This URL must point to YOUR BLAZOR APP's reset page
            var resetLink = $"https://localhost:7019/reset-password?email={model.Email}&token={encodedToken}";

            var emailBody = $"<h1>Password Reset</h1><p>Please reset your password by <a href='{resetLink}'>clicking here</a>.</p>";

            await _emailSender.SendEmailAsync(model.Email, "Reset Your Library Password", emailBody);

            return Ok(new { Message = "If an account with this email exists, a password reset link has been sent." });
        }

        // --- NEW ENDPOINT 2: RESET PASSWORD ---
        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Again, don't reveal user non-existence
                return BadRequest(new { Message = "Error resetting password." });
            }


            // --- THE FIX: We no longer manually UrlDecode the token ---
            // We pass the token directly from the model to the ResetPasswordAsync method.
            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.Password);

            if (result.Succeeded)
            {
                return Ok(new { Message = "Password has been reset successfully." });
            }

            return BadRequest(new { Message = "Error resetting password. The reset link may have been invalid or expired." });
        }

        [HttpPost]
        [Route("change-password")]
        [Authorize] // This ensures only a logged-in user can call this method
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel model)
        {
            // Find the currently logged-in user
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            if (user == null)
            {
                return Unauthorized(); // Should not happen if [Authorize] is working
            }

            // Change the password
            var result = await _userManager.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);

            if (result.Succeeded)
            {
                return Ok(new { Status = "Success", Message = "Password changed successfully!" });
            }
            else
            {
                // Return the errors so the frontend can display them
                var errors = result.Errors.Select(e => e.Description);
                return BadRequest(new { Status = "Error", Message = "Password change failed.", Errors = errors });
            }
        }
        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetUserProfile()
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            if (user == null)
            {
                return Unauthorized();
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            var userLoans = await _context.Loans
                .Where(l => l.ApplicationUserId == user.Id && l.ReturnDate == null)
                .ToListAsync();

            var overdueLoans = userLoans.Where(l => l.DueDate < DateTime.UtcNow).ToList();

            decimal potentialFines = 0;
            const decimal FinePerDay = 0.25m; // Make sure this matches the constant in LoansController

            foreach (var loan in overdueLoans)
            {
                var daysOverdue = (int)(DateTime.UtcNow - loan.DueDate).TotalDays;
                if (daysOverdue > 0)
                {
                    potentialFines += daysOverdue * FinePerDay;
                }
            }

            var profileData = new ProfileDto
            {
                Username = user.UserName,
                Email = user.Email,
                ProfilePictureUrl = user.ProfilePictureUrl,
                Role = userRoles.FirstOrDefault() ?? "N/A",
                CurrentLoansCount = userLoans.Count,
                OverdueLoansCount = overdueLoans.Count,
                PotentialFines = potentialFines
            };

            return Ok(profileData);
        }
        // DTOs for the new endpoints
        public class ForgotPasswordDto { public string Email { get; set; } }
        public class ResetPasswordDto
        {
            public string Email { get; set; }
            public string Token { get; set; }
            public string Password { get; set; }
            [Compare("Password")]
            public string ConfirmPassword { get; set; }
        }

    }
}
