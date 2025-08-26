using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.API.DTOs
{
    public class IssueRequest
    {
        [Required]
        public int BookId { get; set; }

        [Required]
        public string UserId { get; set; }
    }
}