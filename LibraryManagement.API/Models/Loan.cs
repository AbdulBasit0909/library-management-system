using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryManagement.API.Models
{
    public class Loan
    {
        public int Id { get; set; }

        [Required]
        public int BookId { get; set; }

        [Required]
        public string ApplicationUserId { get; set; }

        [Required]
        public DateTime LoanDate { get; set; }

        [Required]
        public DateTime DueDate { get; set; }

        public DateTime? ReturnDate { get; set; }

        // --- ADD THIS NEW PROPERTY ---
        // We will store the fine as a decimal for currency. Defaults to 0.
        [Column(TypeName = "decimal(18, 2)")]
        public decimal FineAmount { get; set; } = 0;

        // --- ADD THIS NEW PROPERTY ---
        // This will track if the fine has been settled. Default to false.
        public bool FinePaid { get; set; } = false;

        public int RenewalCount { get; set; } = 0;
        // Navigation properties
        [ForeignKey("BookId")]
        public virtual Book Book { get; set; }

        [ForeignKey("ApplicationUserId")]
        public virtual ApplicationUser ApplicationUser { get; set; }
    }
}