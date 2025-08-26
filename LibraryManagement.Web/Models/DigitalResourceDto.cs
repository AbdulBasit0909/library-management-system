namespace LibraryManagement.Web.Models
{
    // This represents a resource as it's displayed in the list.
    public class DigitalResourceDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Author { get; set; }
        public string? Subject { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
    }
}