using Blazored.LocalStorage;
using Blazored.Toast;
using LibraryManagement.Web;
using LibraryManagement.Web.Auth;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using LibraryManagement.Web.Services;
using System.Net.Http.Json;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// ?? Load environment-specific API URL from config
using var http = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
var config = await http.GetFromJsonAsync<Dictionary<string, string>>(
    $"appsettings.{builder.HostEnvironment.Environment}.json");

// ?? Register HttpClient with correct API URL
builder.Services.AddScoped(sp => new HttpClient
{
    BaseAddress = new Uri(config["ApiBaseUrl"])
});

// Register all required services
builder.Services.AddBlazoredToast();
builder.Services.AddBlazoredLocalStorage();
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<AuthenticationStateProvider, ApiAuthenticationStateProvider>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<WishlistStateService>();
builder.Services.AddScoped<NotificationStateService>();

await builder.Build().RunAsync();
