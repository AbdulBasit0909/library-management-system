using System.Collections.Generic;

namespace LibraryManagement.Web.Models
{
    // A generic DTO to hold data for any simple chart
    public class ChartDataDto
    {
        public List<string> Labels { get; set; } = new();
        public List<int> Data { get; set; } = new();
    }
}