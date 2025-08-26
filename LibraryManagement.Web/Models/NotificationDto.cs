namespace LibraryManagement.Web.Models
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; }
        public DateTime DateCreated { get; set; }
        public string? Url { get; set; }
    }
}