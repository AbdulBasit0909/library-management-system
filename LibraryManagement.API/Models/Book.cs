using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace LibraryManagement.API.Models
{
    public class Book
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [StringLength(100)]
        public string Author { get; set; }

        [StringLength(20)]
        public string? ISBN { get; set; }

        public DateTime PublishedDate { get; set; }

        [Range(0, int.MaxValue)]
        public int Quantity { get; set; } // Total copies of the book

        // This is the foreign key that links to the Category table.
        public int? CategoryId { get; set; }

        // This is the "navigation property" that EF uses to connect the objects.
        [ForeignKey("CategoryId")]
        public virtual Category Category { get; set; }

        // --- ADD THIS PROPERTY ---
        // A longer text field for the book's summary.
        public string Description { get; set; }

        // --- ADD THIS PROPERTY ---
        // A string to hold the URL to the book's cover image.
        [StringLength(500)]
        public string CoverImageUrl { get; set; }
    }
}