using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace LibraryManagement.API.Hubs
{
    public class NotificationHub : Hub
    {
        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"--> SignalR Client Connected: {Context.ConnectionId}");

            if (Context.User?.Identity?.IsAuthenticated == true)
            {
                var userName = Context.User.Identity.Name;
                if (!string.IsNullOrEmpty(userName))
                {
                    // Add user to their own personal group (for personal notifications)
                    await Groups.AddToGroupAsync(Context.ConnectionId, userName);
                    Console.WriteLine($"--> Authenticated user '{userName}' connected. Added to personal group '{userName}'.");

                    // If the user is a librarian, also add them to the shared "LibrariansGroup"
                    if (Context.User.IsInRole("Librarian"))
                    {
                        await Groups.AddToGroupAsync(Context.ConnectionId, "LibrariansGroup");
                        Console.WriteLine($"--> User '{userName}' is a Librarian. Also added to LibrariansGroup.");
                    }
                }
            }
            else
            {
                Console.WriteLine("--> Anonymous user connected. Not added to any group.");
            }

            await base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine($"--> SignalR Client Disconnected: {Context.ConnectionId}");
            return base.OnDisconnectedAsync(exception);
        }
    }
}