using Blazored.LocalStorage;
using Blazored.Toast;
using LibraryManagement.Web;
using LibraryManagement.Web.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using LibraryManagement.Web.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// This registers a shared HttpClient for the entire app.
builder.Services.AddScoped(sp => new HttpClient
{
    // Make sure this port matches your running API project.
    BaseAddress = new Uri("https://localhost:7075/")
});

// Register all required services for the application.
builder.Services.AddBlazoredToast();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, ApiAuthenticationStateProvider>();

// Register the SignalR service.
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<WishlistStateService>(); // Our new wishlist state service

// THIS IS THE MOST IMPORTANT LINE FOR THE BELL ICON.
// It registers the service that manages the bell's data.
// If this line is missing, the bell will not render.
builder.Services.AddScoped<NotificationStateService>();

await builder.Build().RunAsync();