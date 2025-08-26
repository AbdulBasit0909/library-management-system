using Blazored.LocalStorage;
using LibraryManagement.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

namespace LibraryManagement.Web.Auth
{
    public class ApiAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;
        private readonly WishlistStateService _wishlistStateService;

        public ApiAuthenticationStateProvider(HttpClient httpClient, ILocalStorageService localStorage, WishlistStateService wishlistStateService)
        {
            _httpClient = httpClient;
            _localStorage = localStorage;
            _wishlistStateService = wishlistStateService;

        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            var savedToken = await _localStorage.GetItemAsync<string>("authToken");
            if (string.IsNullOrWhiteSpace(savedToken))
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", savedToken);
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(ParseClaimsFromJwt(savedToken), "jwt")));
        }

        public void MarkUserAsAuthenticated(string token)
        {
            var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(ParseClaimsFromJwt(token), "jwt"));
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", token);
            var authState = Task.FromResult(new AuthenticationState(authenticatedUser));
            NotifyAuthenticationStateChanged(authState);
            _wishlistStateService.InitializeAsync();
        }

        public void MarkUserAsLoggedOut()
        {
            _wishlistStateService.Clear();
            _httpClient.DefaultRequestHeaders.Authorization = null;
            var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
            var authState = Task.FromResult(new AuthenticationState(anonymousUser));
            NotifyAuthenticationStateChanged(authState);
        }

        public IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
        {
            var claims = new List<Claim>();
            var payload = jwt.Split('.')[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);
            if (keyValuePairs != null)
            {
                var roleClaimKeys = new[] { "role", "http://schemas.microsoft.com/ws/2008/06/identity/claims/role" };
                foreach (var key in roleClaimKeys)
                {
                    if (keyValuePairs.TryGetValue(key, out var roles))
                    {
                        if (roles is JsonElement rolesElement && rolesElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var role in rolesElement.EnumerateArray()) claims.Add(new Claim(ClaimTypes.Role, role.ToString()));
                        }
                        else
                        {
                            claims.Add(new Claim(ClaimTypes.Role, roles.ToString()));
                        }
                        keyValuePairs.Remove(key);
                        break;
                    }
                }
                foreach (var kvp in keyValuePairs) claims.Add(new Claim(kvp.Key, kvp.Value.ToString()));
            }
            return claims;
        }

        private byte[] ParseBase64WithoutPadding(string base64)
        {
            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }
            return Convert.FromBase64String(base64);
        }
    }
}