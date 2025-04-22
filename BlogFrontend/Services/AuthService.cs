// BlogFrontend/Services/AuthService.cs
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.IdentityModel.Tokens.Jwt;
using Blazored.LocalStorage;
using BlogFrontend.Models;
using Microsoft.JSInterop;
using System.Text.Json; // For parsing the metadata JSON

namespace BlogFrontend.Services
{
    public interface IAuthService
    {
        Task<string> GetTokenAsync();
        Task<string> GetUserIdAsync();
        Task<string> GetUserNameAsync();
        Task<LoginResponse?> Login(string email, string password);
        Task<MessageResponse?> Register(string email, string password, string name);
        Task<MessageResponse?> Logout();
        Task<AuthCheckResponse?> CheckAuth(string token);
        Task<MessageResponse?> ForgotPassword(string email);
        Task<MessageResponse?> ResetPassword(string token, string newPassword, string confirmPassword);
    }

    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;
        private readonly IJSRuntime _jsRuntime;
        private const string BaseUrl = "api/auth";
        private const string AuthTokenKey = "authToken";

        public AuthService(HttpClient httpClient, ILocalStorageService localStorage, IJSRuntime jsRuntime)
        {
            _httpClient = httpClient;
            _localStorage = localStorage;
            _jsRuntime = jsRuntime;
        }

        public async Task<string> GetTokenAsync()
        {
            return await _localStorage.GetItemAsync<string>(AuthTokenKey);
        }

        public async Task<string> GetUserIdAsync()
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrEmpty(token)) return string.Empty;

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.Claims.FirstOrDefault(c => c.Type == "nameid")?.Value ?? string.Empty;
        }

        public async Task<string> GetUserNameAsync()
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrEmpty(token)) return string.Empty;

            var handler = new JwtSecurityTokenHandler();
            var jwtToken = handler.ReadJwtToken(token);
            return jwtToken.Claims.FirstOrDefault(c => c.Type == "name")?.Value ?? string.Empty;
        }

        public async Task<LoginResponse?> Login(string email, string password)
        {
            var response = await _httpClient.PostAsync(
                $"{BaseUrl}/login?email={Uri.EscapeDataString(email)}&password={Uri.EscapeDataString(password)}",
                null);

            if (response.IsSuccessStatusCode)
            {
                var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
                if (loginResponse != null && !string.IsNullOrEmpty(loginResponse.AccessToken))
                {
                    await _localStorage.SetItemAsync(AuthTokenKey, loginResponse.AccessToken);
                    _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", loginResponse.AccessToken);
                    Console.WriteLine($"Token stored: {loginResponse.AccessToken}"); // Add for debugging
                }
                return loginResponse;
            }
            Console.WriteLine($"Login failed: {response.StatusCode}"); // Add for debugging
            return null;
        }

        public async Task<MessageResponse?> Register(string email, string password, string name)
        {
            var response = await _httpClient.PostAsync(
                $"{BaseUrl}/register?email={Uri.EscapeDataString(email)}&password={Uri.EscapeDataString(password)}&name={Uri.EscapeDataString(name)}",
                null);
            return await response.Content.ReadFromJsonAsync<MessageResponse>();
        }

        public async Task<MessageResponse?> Logout()
        {
            var response = await _httpClient.PostAsync($"{BaseUrl}/logout", null);
            await _localStorage.RemoveItemAsync(AuthTokenKey);
            return await response.Content.ReadFromJsonAsync<MessageResponse>();
        }

        public async Task<AuthCheckResponse?> CheckAuth(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            var response = await _httpClient.GetAsync($"{BaseUrl}/check?accessToken={Uri.EscapeDataString(token)}");

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<AuthCheckResponse>();
            }
            return null;
        }

        public async Task<MessageResponse?> ForgotPassword(string email)
        {
            var response = await _httpClient.PostAsync(
                $"{BaseUrl}/forgot-password?email={Uri.EscapeDataString(email)}",
                null);
            return await response.Content.ReadFromJsonAsync<MessageResponse>();
        }

        public async Task<MessageResponse?> ResetPassword(string token, string newPassword, string confirmPassword)
        {
            var response = await _httpClient.PostAsync(
                $"{BaseUrl}/reset-password?accessToken={Uri.EscapeDataString(token)}&newPassword={Uri.EscapeDataString(newPassword)}&confirmPassword={Uri.EscapeDataString(confirmPassword)}",
                null);
            return await response.Content.ReadFromJsonAsync<MessageResponse>();
        }
    }
}