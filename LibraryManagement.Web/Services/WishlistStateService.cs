using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace LibraryManagement.Web.Services
{
    public class WishlistStateService
    {
        private readonly HttpClient _httpClient;
        private HashSet<int> _wishlistBookIds = new();

        public WishlistStateService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Fetches the initial list of IDs when a user logs in
        public async Task InitializeAsync()
        {
            try
            {
                var ids = await _httpClient.GetFromJsonAsync<List<int>>("api/wishlist/ids");
                _wishlistBookIds = new HashSet<int>(ids ?? new List<int>());
            }
            catch
            {
                // Silently fail if the user is not logged in or an error occurs
                _wishlistBookIds = new HashSet<int>();
            }
        }

        // Checks if a book is on the list
        public bool IsOnWishlist(int bookId) => _wishlistBookIds.Contains(bookId);

        // Adds a book ID to the local state
        public void AddBook(int bookId) => _wishlistBookIds.Add(bookId);

        // Removes a book ID from the local state
        public void RemoveBook(int bookId) => _wishlistBookIds.Remove(bookId);

        // Clears the state on logout
        public void Clear() => _wishlistBookIds.Clear();
    }
}