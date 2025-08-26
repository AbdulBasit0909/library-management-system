using System;
using System.ComponentModel.DataAnnotations;

namespace LibraryManagement.API.Models
{
    public class DigitalResource
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [StringLength(100)]
        public string Author { get; set; }

        [StringLength(100)]
        public string Subject { get; set; }

        // The name of the file as it's stored on the server (e.g., "guid.pdf")
        [Required]
        public string StoredFileName { get; set; }

        // The original name of the file the user uploaded (e.g., "my-syllabus.pdf")
        [Required]
        public string OriginalFileName { get; set; }

        public DateTime DateUploaded { get; set; } = DateTime.UtcNow;
    }
}