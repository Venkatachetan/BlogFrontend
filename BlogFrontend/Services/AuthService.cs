using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using Blazored.LocalStorage;
using BlogFrontend.Models;
using Microsoft.JSInterop;
using System.Net.Http;
using Microsoft.AspNetCore.Components.Forms;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BlogFrontend.Services
{
    public interface IAuthService
    {
        Task<string> GetTokenAsync();
        Task<string> GetUserIdAsync();
        Task<string> GetUserNameAsync();
        Task<LoginResponse?> Login(string email, string password);
        Task<MessageResponse?> Register(string email, string password, string name);
        Task<LoginResponse?> VerifyOtp(string email, string otp);
        Task<MessageResponse?> Logout();
        Task<AuthCheckResponse?> CheckAuth(string token);
        Task<MessageResponse?> ForgotPassword(string email);
        Task<MessageResponse?> ResetPassword(string token, string newPassword, string confirmPassword);
        Task<ProfilePictureResponse?> UploadProfilePictureAsync(IBrowserFile profilePicture);
        Task<string?> GetProfilePictureAsync(string userId);
        Task<bool> IsAuthenticated();
        Task<string> GetCurrentUserIdAsync();
    }

    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;
        private readonly IJSRuntime _jsRuntime;
        private readonly ILogger<AuthService> _logger;
        private readonly NavigationManager _navigationManager;
        private readonly IConfiguration _configuration;
        private const string BaseUrl = "api/auth";
        private const string AuthTokenKey = "authToken";

        public AuthService(
            HttpClient httpClient, 
            ILocalStorageService localStorage, 
            IJSRuntime jsRuntime, 
            ILogger<AuthService> logger, 
            NavigationManager navigationManager, 
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _localStorage = localStorage;
            _jsRuntime = jsRuntime;
            _logger = logger;
            _navigationManager = navigationManager;
            _configuration = configuration;
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
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/auth/login", new { email, password });

                if (response.IsSuccessStatusCode)
                {
                    var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
                    if (loginResponse?.AccessToken != null)
                    {
                        await _localStorage.SetItemAsync(AuthTokenKey, loginResponse.AccessToken);
                        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResponse.AccessToken);
                    }
                    return loginResponse;
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Login error: {ex.Message}");
                return null;
            }
        }
        public async Task<MessageResponse?> Register(string email, string password, string name)
        {
            try
            {
                using var content = new MultipartFormDataContent();
                content.Add(new StringContent(email), "email");
                content.Add(new StringContent(password), "password");
                content.Add(new StringContent(name ?? ""), "name");

                var response = await _httpClient.PostAsync($"{BaseUrl}/register", content);
                var result = await response.Content.ReadFromJsonAsync<MessageResponse>();

                Console.WriteLine($"Register response for {email}: {result?.Message}");
                _logger?.LogInformation($"Register response for {email}: {result?.Message}");

                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Register error: {ex.Message}");
                return null;
            }
        }
        public async Task<LoginResponse?> VerifyOtp(string email, string otp)
        {
            try
            {
                var request = new OtpVerificationRequest { Email = email, Otp = otp };
                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/verify-otp", request);

                if (response.IsSuccessStatusCode)
                {
                    var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponse>();
                    if (loginResponse?.AccessToken != null)
                    {
                        await _localStorage.SetItemAsync(AuthTokenKey, loginResponse.AccessToken);
                        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loginResponse.AccessToken);
                    }
                    return loginResponse;
                }
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OTP verification error: {ex.Message}");
                return null;
            }
        }

        public async Task<MessageResponse?> Logout()
        {
            await _localStorage.RemoveItemAsync(AuthTokenKey);
            _httpClient.DefaultRequestHeaders.Authorization = null;
            return new MessageResponse { Message = "Logged out successfully" };
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
            try
            {
                var response = await _httpClient.PostAsync(
                    $"{BaseUrl}/forgot-password?email={Uri.EscapeDataString(email)}",
                    null);
                return await response.Content.ReadFromJsonAsync<MessageResponse>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ForgotPassword error: {ex.Message}");
                return null;
            }
        }

        public async Task<MessageResponse?> ResetPassword(string token, string newPassword, string confirmPassword)
        {
            try
            {
                var requestBody = new
                {
                    accessToken = token,
                    newPassword,
                    confirmPassword
                };

                var response = await _httpClient.PostAsJsonAsync($"{BaseUrl}/reset-password", requestBody);
                return await response.Content.ReadFromJsonAsync<MessageResponse>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ResetPassword error: {ex.Message}");
                return null;
            }
        }

        public async Task<ProfilePictureResponse?> UploadProfilePictureAsync(IBrowserFile profilePicture)
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrEmpty(token))
                throw new Exception("No authentication token available.");

            _httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            using var formData = new MultipartFormDataContent();
            var imageContent = new StreamContent(profilePicture.OpenReadStream(maxAllowedSize: 10485760));
            imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(profilePicture.ContentType);
            formData.Add(imageContent, "profilePicture", profilePicture.Name);

            var response = await _httpClient.PostAsync($"{BaseUrl}/upload-profile-picture", formData);

            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<ProfilePictureResponse>();
            }

            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to upload profile picture: {error}");
        }

        public async Task<string?> GetProfilePictureAsync(string userId)
        {
            var response = await _httpClient.GetAsync($"{BaseUrl}/profile-picture/{userId}");

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<ProfilePictureResponse>();
                return result?.ImageBase64;
            }

            var error = await response.Content.ReadAsStringAsync();
            throw new Exception($"Failed to get profile picture: {error}");
        }

        public async Task<bool> IsAuthenticated()
        {
            var token = await _jsRuntime.InvokeAsync<string>("localStorage.getItem", "authToken");
            return !string.IsNullOrEmpty(token);
        }

        public async Task<string> GetCurrentUserIdAsync()
        {
            var token = await GetTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                throw new Exception("No authentication token available.");
            }

            var response = await _httpClient.GetAsync($"{BaseUrl}/check?accessToken={Uri.EscapeDataString(token)}");

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<AuthCheckResponse>(jsonString, options);
                return result?.Id ?? throw new Exception("User ID not found in token validation response.");
            }

            throw new Exception($"Failed to validate token: {await response.Content.ReadAsStringAsync()}");
        }
    }
}