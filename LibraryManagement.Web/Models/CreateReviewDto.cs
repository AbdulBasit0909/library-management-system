using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.Web.Models
{
    public class CreateReviewDto
    {
        public int BookId { get; set; }

        [Range(1, 5, ErrorMessage = "Please select a star rating.")]
        public int Rating { get; set; }

        [StringLength(2000)]
        public string Comment { get; set; } = string.Empty;
    }
}