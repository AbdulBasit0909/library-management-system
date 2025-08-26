namespace LibraryManagement.Web.Models
{
    public class ProfileDto
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string Role { get; set; }
        public int CurrentLoansCount { get; set; }
        public int OverdueLoansCount { get; set; }
        public decimal PotentialFines { get; set; }
        public string? ProfilePictureUrl { get; set; }
    }
}