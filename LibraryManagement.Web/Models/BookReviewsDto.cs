using System.Collections.Generic;

namespace LibraryManagement.Web.Models
{
    // Represents the entire payload from the API: the average and the list.
    public class BookReviewsDto
    {
        public double AverageRating { get; set; }
        public List<ReviewDto> Reviews { get; set; } = new();
    }
}