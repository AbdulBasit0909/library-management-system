namespace LibraryManagement.API.DTOs
{
    public class LoanDto
    {
        public int Id { get; set; }
        public string BookTitle { get; set; }
        public int BookId { get; set; }
        public string UserName { get; set; }
        public DateTime LoanDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal FineAmount { get; set; }
    }
}
    
