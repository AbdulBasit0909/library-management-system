using LibraryManagement.Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace LibraryManagement.Web.Services
{
    public class NotificationStateService
    {
        private readonly HttpClient _httpClient;
        public List<NotificationDto> Notifications { get; private set; } = new();
        public int UnreadCount { get; private set; } = 0;

        // This event will be triggered whenever the notification state changes
        public event Action? OnChange;

        public NotificationStateService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        // Fetches the initial state from the API
        public async Task InitializeAsync()
        {
            await GetUnreadCountAsync();
            await GetNotificationsAsync();
        }

        public async Task GetUnreadCountAsync()
        {
            try
            {
                var result = await _httpClient.GetFromJsonAsync<UnreadCountDto>("api/notifications/unread-count");
                UnreadCount = result?.Count ?? 0;
                NotifyStateChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching unread count: {ex.Message}");
            }
        }

        public async Task GetNotificationsAsync()
        {
            try
            {
                var result = await _httpClient.GetFromJsonAsync<List<NotificationDto>>("api/notifications");
                Notifications = result ?? new List<NotificationDto>();
                NotifyStateChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching notifications: {ex.Message}");
            }
        }

        public async Task MarkAllAsReadAsync()
        {
            try
            {
                await _httpClient.PostAsync("api/notifications/mark-all-as-read", null);
                UnreadCount = 0;
                foreach (var notification in Notifications)
                {
                    notification.IsRead = true;
                }
                NotifyStateChanged();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error marking notifications as read: {ex.Message}");
            }
        }

        // Helper method to raise the OnChange event
        private void NotifyStateChanged() => OnChange?.Invoke();

        // Helper DTO class to match the API response
        private class UnreadCountDto { public int Count { get; set; } }
    }
}