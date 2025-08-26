using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagement.API.Models
{
    public class WishlistItem
    {
        // Foreign key to the User
        public string ApplicationUserId { get; set; }

        // Foreign key to the Book
        public int BookId { get; set; }

        // Navigation properties
        [ForeignKey("ApplicationUserId")]
        public virtual ApplicationUser User { get; set; }

        [ForeignKey("BookId")]
        public virtual Book Book { get; set; }
    }
}