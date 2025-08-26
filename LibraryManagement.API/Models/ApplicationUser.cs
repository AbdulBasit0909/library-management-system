using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
namespace LibraryManagement.API.Models
{
   
    public class ApplicationUser : IdentityUser
    {
        public virtual ICollection<Loan> Loans { get; set; }
    
       // --- ADD THIS LINE ---
        public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    
        // This will store the relative path to the user's profile picture.
        // It's nullable because new users won't have a picture yet.
        public string? ProfilePictureUrl { get; set; }
    }
}