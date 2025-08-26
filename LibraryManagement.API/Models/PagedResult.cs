namespace LibraryManagement.API.Models
{
    // A generic class to hold a "page" of data and its metadata
    public class PagedResult<T>
    {
        // The items for the current page
        public List<T> Items { get; set; }

        // The total number of records in the database
        public int TotalCount { get; set; }

        // The total number of pages available
        public int TotalPages { get; set; }

        // The current page number
        public int CurrentPage { get; set; }

        // The number of items per page
        public int PageSize { get; set; }
    }
}
