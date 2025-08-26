using Blazored.LocalStorage;
using Blazored.Toast.Services;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Threading.Tasks;

namespace LibraryManagement.Web.Services
{
    public class NotificationService : IAsyncDisposable
    {
        private readonly IToastService _toastService;
        private readonly ILocalStorageService _localStorage;
        private readonly NotificationStateService _notificationStateService;
        private HubConnection? _hubConnection;

        // The constructor now correctly receives ILocalStorageService and the new NotificationStateService
        public NotificationService(
            IToastService toastService,
            ILocalStorageService localStorage, // <--- THIS WAS THE FIX
            NotificationStateService notificationStateService)
        {
            _toastService = toastService;
            _localStorage = localStorage;
            _notificationStateService = notificationStateService;
        }

        public async Task StartConnectionAsync()
        {
            // If connection is already established and running, do nothing.
            if (_hubConnection != null && _hubConnection.State == HubConnectionState.Connected)
            {
                return;
            }

            // Get the authentication token from local storage.
            var token = await _localStorage.GetItemAsync<string>("authToken");
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("--> SignalR: No auth token found. Connection will not be started.");
                return; // Silently fail if the user is not logged in.
            }

            // Build the HubConnection, providing the token for authentication.
            _hubConnection = new HubConnectionBuilder()
                .WithUrl("https://localhost:7075/notificationhub", options =>
                {
                    // This option ensures the token is sent with the connection request.
                    options.AccessTokenProvider = () => Task.FromResult(token);
                })
                .WithAutomaticReconnect()
                .Build();

            // Define the client-side method that the server will call.
            _hubConnection.On<string, string>("ReceiveNotification", async (message, type) =>
            {
                Console.WriteLine($"--> SignalR NOTIFICATION RECEIVED! Type: '{type}', Message: '{message}'");

                // 1. Show the immediate toast pop-up
                switch (type)
                {
                    case "success": _toastService.ShowSuccess(message); break;
                    case "warning": _toastService.ShowWarning(message); break;
                    case "info": _toastService.ShowInfo(message); break;
                    default: _toastService.ShowInfo(message); break;
                }

                // 2. Tell the state service to refresh the persistent notification list and count
                await _notificationStateService.InitializeAsync();
            });

            // Start the connection.
            try
            {
                await _hubConnection.StartAsync();
                Console.WriteLine("--> SignalR Connection for Authenticated User SUCCESSFUL.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"--> SignalR Connection for Authenticated User FAILED: {ex.Message}");
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (_hubConnection != null)
            {
                await _hubConnection.DisposeAsync();
            }
        }
    }
}