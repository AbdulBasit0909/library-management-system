using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Web.Models
{
    public class Book
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required")]
        [StringLength(200)]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Author is required")]
        [StringLength(100)]
        public string Author { get; set; } = string.Empty;

        [StringLength(20)]
        public string? ISBN { get; set; }

        public DateTime PublishedDate { get; set; } = DateTime.Now;

        [Required]
        [Range(0, int.MaxValue, ErrorMessage = "Quantity must be a positive number")]
        public int Quantity { get; set; }
        // Use nullable int so "No Category" is an option.
        public int? CategoryId { get; set; }

        // --- ADD THESE PROPERTIES ---
        public string Description { get; set; } = string.Empty;
        public string CoverImageUrl { get; set; } = string.Empty;

    }
}