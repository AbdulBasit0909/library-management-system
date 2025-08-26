namespace LibraryManagement.API.DTOs
{
    public class DashboardStatsDto
    {
        public int TotalBooks { get; set; }
        public int TotalUsers { get; set; }
        public int BooksOnLoan { get; set; }
        public int OverdueBooks { get; set; }
    }
}