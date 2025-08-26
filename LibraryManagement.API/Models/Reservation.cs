using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagement.API.Models
{
    public class Reservation
    {
        public int Id { get; set; }

        [Required]
        public int BookId { get; set; }

        [Required]
        public string ApplicationUserId { get; set; }

        [Required]
        public DateTime RequestDate { get; set; }

        // Status can be "Pending", "Approved", "Rejected"
        [Required]
        public string Status { get; set; } = "Pending";

        // Navigation properties
        [ForeignKey("BookId")]
        public virtual Book Book { get; set; }

        [ForeignKey("ApplicationUserId")]
        public virtual ApplicationUser ApplicationUser { get; set; }
    }
}