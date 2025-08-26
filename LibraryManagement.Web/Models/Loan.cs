namespace LibraryManagement.Web.Models
{
    public class Loan
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public string ApplicationUserId { get; set; }
        public DateTime LoanDate { get; set; }
        public DateTime DueDate { get; set; }
        public DateTime? ReturnDate { get; set; }
        public Book? Book { get; set; }
        public User? ApplicationUser { get; set; }
    }
}