namespace LibraryManagement.Web.Models
{
    public class ReservationDto
    {
        public int ReservationId { get; set; }
        public string BookTitle { get; set; }
        public string UserName { get; set; }
        public DateTime RequestDate { get; set; }
    }
}