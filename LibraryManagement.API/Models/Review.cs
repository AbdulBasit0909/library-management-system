using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagement.API.Models
{
    public class Review
    {
        public int Id { get; set; }

        [Required]
        [Range(1, 5)] // Enforce a 1-5 star rating
        public int Rating { get; set; }

        [StringLength(2000)] // Limit the length of a review
        public string Comment { get; set; }

        public DateTime DatePosted { get; set; } = DateTime.UtcNow;

        // Foreign keys
        [Required]
        public int BookId { get; set; }

        [Required]
        public string ApplicationUserId { get; set; }

        // Navigation properties
        [ForeignKey("BookId")]
        public virtual Book Book { get; set; }

        [ForeignKey("ApplicationUserId")]
        public virtual ApplicationUser User { get; set; }
    }
}