namespace LibraryManagement.Web.Models
{
    // Represents a single review comment.
    public class ReviewDto
    {
        public int Id { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; } = string.Empty;
        public DateTime DatePosted { get; set; }
        public string UserName { get; set; } = string.Empty;
    }
}