using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.API.DTOs
{
    // This DTO represents the data needed to create or update a book.
    // Notice it has no navigation properties, just simple data types.
    public class BookRequestDto
    {
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
        public int Quantity { get; set; }

        // It only contains the foreign key ID, which is what the frontend sends.
        public int? CategoryId { get; set; }

        // --- ADD THIS PROPERTY ---
        public string Description { get; set; }

        // --- ADD THIS PROPERTY ---
        [StringLength(500)]
        public string CoverImageUrl { get; set; }
    }
}