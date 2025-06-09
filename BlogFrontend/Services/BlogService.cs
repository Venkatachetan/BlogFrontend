using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using BlogFrontend.Models;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.SignalR.Client;

namespace BlogFrontend.Services
{
    public interface IBlogService
    {
        Task<List<Post>> GetAllPostsAsync();
        Task<List<Post>> GetUserPostsAsync(string userId);
        Task<Post> GetPostByIdAsync(string postId);
        Task<Post> CreatePostAsync(string title, string content, List<string> tags, IBrowserFile image);
        Task<bool> AddCommentAsync(string postId, string text);
        Task<bool> DeletePostAsync(string postId);
        Task<Post> UpdatePostAsync(string postId, string title, string content, IBrowserFile image, List<string> tags);
        Task<bool> DeleteCommentAsync(string postId, string commentId);
        Task<List<Post>> GetFollowedPostsAsync();
        Task<List<string>> GetFollowingIdsAsync();
        Task<OperationResult> FollowUserAsync(string userIdToFollow);
        Task<OperationResult> UnfollowUserAsync(string userIdToUnfollow);
        Task<bool> IsFollowingAsync(string userIdToCheck);
        Task<List<User>> SearchUsersAsync(string query);
        Task<string> GetCurrentUserIdAsync();
        Task<Post> LikePostAsync(string postId);
        Task<Post> UnlikePostAsync(string postId);
        Task<List<User>> GetFollowersAsync(string userId);
        Task<List<User>> GetFollowingAsync(string userId);
        Task<List<Post>> GetRandomPostsAsync(int count);
        Task<ProfilePictureResponse?> UploadProfilePictureAsync(IBrowserFile profilePicture);
        Task<string?> GetProfilePictureAsync(string userId);
        Task<UserProfile> GetUserProfileAsync(string userId);
        Task InitializeSignalRAsync();
        event Action<string> OnNotificationReceived;
        Task<(List<Post> posts, long totalCount)> GetAllPostsPaginatedAsync(int page, int pageSize);
        Task<(List<Post> Posts, long TotalCount)> GetUserPostsPaginatedAsync(string userId, int page, int pageSize);
        Task<(List<Post> Posts, long TotalCount)> GetFollowedPostsPaginatedAsync(int page, int pageSize);
        Task<bool> UpdateBioAsync(string bio);
        Task<List<SearchResult>> SearchContentAsync(string query);
        Task<CombinedSearchResult> PerformCombinedSearchAsync(string query);
    }

    public class BlogService : IBlogService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;
        private readonly NavigationManager _navigationManager;
        private readonly IJSRuntime _jsRuntime;
        private readonly IConfiguration _configuration;
        private readonly NotificationService _notificationService;
        private HubConnection _hubConnection;
        private const string BaseUrl = "api/blog";
        private const string AuthUrl = "api/auth";

        public event Action<string> OnNotificationReceived;

        public BlogService(HttpClient httpClient, IAuthService authService, NavigationManager navigationManager, IJSRuntime jsRuntime, IConfiguration configuration, NotificationService notificationService)
        {
            _httpClient = httpClient;
            _authService = authService;
            _navigationManager = navigationManager;
            _jsRuntime = jsRuntime;
            _configuration = configuration;
            _notificationService = notificationService;
        }

        public async Task InitializeSignalRAsync()
        {
            try
            {
                var token = await _authService.GetTokenAsync();
                if (string.IsNullOrEmpty(token))
                {
                    await _jsRuntime.InvokeVoidAsync("console.error", "No authentication token available for SignalR.");
                    throw new Exception("No authentication token available.");
                }

                var backendBaseUrl = _httpClient.BaseAddress?.ToString();
                if (string.IsNullOrEmpty(backendBaseUrl))
                {
                    await _jsRuntime.InvokeVoidAsync("console.error", "HttpClient base address not configured.");
                    throw new Exception("HttpClient base address not configured.");
                }

                var hubUrl = $"{backendBaseUrl.TrimEnd('/')}/notificationHub";
                await _jsRuntime.InvokeVoidAsync("console.log", $"Attempting to connect SignalR to: {hubUrl}");

                _hubConnection = new HubConnectionBuilder()
                    .WithUrl(hubUrl, options =>
                    {
                        options.AccessTokenProvider = () => Task.FromResult(token);
                    })
                    .WithAutomaticReconnect()
                    .Build();

                _hubConnection.On<string>("ReceiveNotification", (message) =>
                {
                    _notificationService.AddNotification(message);
                    OnNotificationReceived?.Invoke(message);
                    _jsRuntime.InvokeVoidAsync("console.log", $"Received notification: {message}");
                });

                _hubConnection.Closed += async (error) =>
                {
                    await _jsRuntime.InvokeVoidAsync("console.log", $"SignalR connection closed: {error?.Message}");
                };

                await _hubConnection.StartAsync();
                await _jsRuntime.InvokeVoidAsync("console.log", "SignalR connection established successfully.");
            }
            catch (Exception ex)
            {
                await _jsRuntime.InvokeVoidAsync("console.error", $"Failed to initialize SignalR: {ex.ToString()}");
                throw;
            }
        }

        private async Task EnsureAuthHeader()
        {
            var token = await _authService.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
                await _jsRuntime.InvokeVoidAsync("console.log", "Setting auth header with token:", token);
            }
            else
            {
                await _jsRuntime.InvokeVoidAsync("console.error", "No authentication token available");
                throw new Exception("No authentication token available.");
            }
        }

        public async Task<List<Post>> GetAllPostsAsync()
        {
            await EnsureAuthHeader();
            var response = await _httpClient.GetAsync($"{BaseUrl}/all");

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var postResponses = JsonSerializer.Deserialize<List<PostResponse>>(jsonString, options) ?? new List<PostResponse>();
                return postResponses.Select(pr =>
                {
                    pr.Post.ImageBase64 = pr.ImageBase64;
                    return pr.Post;
                }).ToList();
            }

            return new List<Post>();
        }

        public async Task<List<Post>> GetUserPostsAsync(string userId)
        {
            await EnsureAuthHeader();
            var response = await _httpClient.GetAsync($"{BaseUrl}/user/{userId}");

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var postResponses = JsonSerializer.Deserialize<List<PostResponse>>(jsonString, options) ?? new List<PostResponse>();
                return postResponses.Select(pr =>
                {
                    pr.Post.ImageBase64 = pr.ImageBase64;
                    return pr.Post;
                }).ToList();
            }

            return new List<Post>();
        }

        public async Task<Post> GetPostByIdAsync(string postId)
        {
            await EnsureAuthHeader();
            var response = await _httpClient.GetAsync($"{BaseUrl}/{postId}");

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<PostResponse>(jsonString, options);
                if (result?.Post != null)
                {
                    result.Post.ImageBase64 = result.ImageBase64;
                    return result.Post;
                }
                return new Post();
            }

            throw new Exception($"Failed to get post: {await response.Content.ReadAsStringAsync()}");
        }

        public async Task<Post> CreatePostAsync(string title, string content, List<string> tags, IBrowserFile image)
        {
            await EnsureAuthHeader();

            var formData = new MultipartFormDataContent();
            formData.Add(new StringContent(title), "Title");
            formData.Add(new StringContent(content), "Content");

            if (tags != null && tags.Any())
            {
                foreach (var tag in tags)
                {
                    formData.Add(new StringContent(tag), "Tags");
                }
            }

            if (image != null)
            {
                var imageContent = new StreamContent(image.OpenReadStream(maxAllowedSize: 10485760));
                imageContent.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);
                formData.Add(imageContent, "Image", image.Name);
            }

            var response = await _httpClient.PostAsync($"{BaseUrl}/create", formData);

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<PostResponse>(jsonString, options);
                if (result?.Post != null)
                {
                    result.Post.ImageBase64 = result.ImageBase64;
                    return result.Post;
                }
                return new Post();
            }

            throw new Exception($"Failed to create post: {await response.Content.ReadAsStringAsync()}");
        }

        public async Task<bool> AddCommentAsync(string postId, string text)
        {
            await EnsureAuthHeader();
            var comment = new { Text = text };
            var content = new StringContent(JsonSerializer.Serialize(comment), Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync($"{BaseUrl}/comment/{postId}", content);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to add comment: {error}");
            }
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeletePostAsync(string postId)
        {
            await EnsureAuthHeader();
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/{postId}");
            return response.IsSuccessStatusCode;
        }

        public async Task<Post> UpdatePostAsync(string postId, string title, string content, IBrowserFile image, List<string> tags)
        {
            await EnsureAuthHeader();

            var formData = new MultipartFormDataContent();
            formData.Add(new StringContent(title), "Title");
            formData.Add(new StringContent(content), "Content");

            if (tags != null && tags.Any())
            {
                foreach (var tag in tags)
                {
                    formData.Add(new StringContent(tag), "Tags");
                }
            }

            if (image != null)
            {
                var imageContent = new StreamContent(image.OpenReadStream(maxAllowedSize: 10485760));
                imageContent.Headers.ContentType = new MediaTypeHeaderValue(image.ContentType);
                formData.Add(imageContent, "Image", image.Name);
            }

            var response = await _httpClient.PutAsync($"{BaseUrl}/{postId}", formData);

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<PostResponse>(jsonString, options);
                if (result?.Post != null)
                {
                    result.Post.ImageBase64 = result.ImageBase64;
                    return result.Post;
                }
                throw new Exception("Failed to deserialize updated post");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                var errorOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(errorContent, errorOptions);

                switch ((int)response.StatusCode)
                {
                    case 400:
                        throw new Exception($"Invalid request: {errorResponse?.Message ?? "Bad request"}");
                    case 401:
                        throw new Exception($"Unauthorized: {errorResponse?.Message ?? "Authentication required"}");
                    case 403:
                        throw new Exception($"Forbidden: {errorResponse?.Message ?? "You lack permission to update this post"}");
                    case 404:
                        throw new Exception($"Post not found: {errorResponse?.Message ?? "The specified post does not exist"}");
                    default:
                        throw new Exception($"Failed to update post: {response.StatusCode} - {errorResponse?.Message ?? errorContent}");
                }
            }
        }

        public async Task<bool> DeleteCommentAsync(string postId, string commentId)
        {
            await EnsureAuthHeader();
            var response = await _httpClient.DeleteAsync($"{BaseUrl}/{postId}/comment/{commentId}");
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to delete comment: {error}");
            }
            return response.IsSuccessStatusCode;
        }

        public async Task<List<Post>> GetFollowedPostsAsync()
        {
            await EnsureAuthHeader();
            var response = await _httpClient.GetAsync($"{BaseUrl}/followed");

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var postResponses = JsonSerializer.Deserialize<List<PostResponse>>(jsonString, options) ?? new List<PostResponse>();
                return postResponses.Select(pr =>
                {
                    pr.Post.ImageBase64 = pr.ImageBase64;
                    return pr.Post;
                }).ToList();
            }

            return new List<Post>();
        }

        public async Task<List<string>> GetFollowingIdsAsync()
        {
            await EnsureAuthHeader();
            var response = await _httpClient.GetAsync($"{BaseUrl}/following/{await GetCurrentUserIdAsync()}");

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var users = JsonSerializer.Deserialize<List<User>>(jsonString, options) ?? new List<User>();
                return users.Select(u => u.UserId).ToList();
            }

            throw new Exception($"Failed to get following IDs: {await response.Content.ReadAsStringAsync()}");
        }

        public async Task<OperationResult> FollowUserAsync(string userIdToFollow)
        {
            try
            {
                await EnsureAuthHeader();
                var response = await _httpClient.PostAsync($"{BaseUrl}/follow/{userIdToFollow}", null);

                if (response.IsSuccessStatusCode)
                {
                    return new OperationResult { Success = true, Message = "Followed successfully" };
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(errorContent, options);

                string errorMessage = errorResponse?.Message ?? $"Failed to follow user: {response.StatusCode}";
                return new OperationResult { Success = false, Message = errorMessage };
            }
            catch (Exception ex)
            {
                return new OperationResult { Success = false, Message = $"Error following user: {ex.Message}" };
            }
        }

        public async Task<OperationResult> UnfollowUserAsync(string userIdToUnfollow)
        {
            try
            {
                await EnsureAuthHeader();
                var response = await _httpClient.PostAsync($"{BaseUrl}/unfollow/{userIdToUnfollow}", null);

                if (response.IsSuccessStatusCode)
                {
                    return new OperationResult { Success = true, Message = "Unfollowed successfully" };
                }

                var errorContent = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(errorContent, options);

                string errorMessage = errorResponse?.Message ?? $"Failed to unfollow user: {response.StatusCode}";
                return new OperationResult { Success = false, Message = errorMessage };
            }
            catch (Exception ex)
            {
                return new OperationResult { Success = false, Message = $"Error unfollowing user: {ex.Message}" };
            }
        }

        public async Task<bool> IsFollowingAsync(string userIdToCheck)
        {
            try
            {
                await EnsureAuthHeader();
                var followingIds = await GetFollowingIdsAsync();
                return followingIds.Contains(userIdToCheck);
            }
            catch (Exception)
            {
                return false;
            }
        }

        public async Task<List<User>> SearchUsersAsync(string query)
        {
            await EnsureAuthHeader();
            var response = await _httpClient.GetAsync($"{BaseUrl}/users/search?query={Uri.EscapeDataString(query)}");

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<List<User>>(jsonString, options) ?? new List<User>();
            }

            throw new Exception($"Failed to search users: {await response.Content.ReadAsStringAsync()}");
        }

        public async Task<string> GetCurrentUserIdAsync()
        {
            var token = await _authService.GetTokenAsync();
            if (string.IsNullOrEmpty(token))
            {
                throw new Exception("No authentication token available.");
            }

            var response = await _httpClient.GetAsync($"{AuthUrl}/check?accessToken={Uri.EscapeDataString(token)}");

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<AuthCheckResponse>(jsonString, options);
                return result?.Id ?? throw new Exception("User ID not found in token validation response.");
            }

            throw new Exception($"Failed to validate token: {await response.Content.ReadAsStringAsync()}");
        }

        public async Task<Post> LikePostAsync(string postId)
        {
            await EnsureAuthHeader();
            var response = await _httpClient.PostAsync($"{BaseUrl}/like/{postId}", null);

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var post = JsonSerializer.Deserialize<Post>(jsonString, options);
                if (post != null)
                {
                    post.ImageBase64 = post.ImageBytes != null ? Convert.ToBase64String(post.ImageBytes) : null;
                    return post;
                }
                throw new Exception("Failed to deserialize post after liking");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to like post: {response.StatusCode} - {errorContent}");
            }
        }

        public async Task<Post> UnlikePostAsync(string postId)
        {
            await EnsureAuthHeader();
            var response = await _httpClient.PostAsync($"{BaseUrl}/unlike/{postId}", null);

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var post = JsonSerializer.Deserialize<Post>(jsonString, options);
                if (post != null)
                {
                    post.ImageBase64 = post.ImageBytes != null ? Convert.ToBase64String(post.ImageBytes) : null;
                    return post;
                }
                throw new Exception("Failed to deserialize post after unliking");
            }
            else
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                throw new Exception($"Failed to unlike post: {response.StatusCode} - {errorContent}");
            }
        }

        public async Task<List<User>> GetFollowersAsync(string userId)
        {
            await EnsureAuthHeader();
            var response = await _httpClient.GetAsync($"{BaseUrl}/followers/{userId}");

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<List<User>>(jsonString, options) ?? new List<User>();
            }

            throw new Exception($"Failed to get followers: {await response.Content.ReadAsStringAsync()}");
        }

        public async Task<List<User>> GetFollowingAsync(string userId)
        {
            await EnsureAuthHeader();
            var response = await _httpClient.GetAsync($"{BaseUrl}/following/{userId}");

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<List<User>>(jsonString, options) ?? new List<User>();
            }

            throw new Exception($"Failed to get following: {await response.Content.ReadAsStringAsync()}");
        }

        public async Task<List<Post>> GetRandomPostsAsync(int count)
        {
            await EnsureAuthHeader();
            var response = await _httpClient.GetAsync($"{BaseUrl}/random?count={count}");

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var postResponses = JsonSerializer.Deserialize<List<PostResponse>>(jsonString, options) ?? new List<PostResponse>();
                return postResponses.Select(pr =>
                {
                    pr.Post.ImageBase64 = pr.ImageBase64;
                    return pr.Post;
                }).ToList();
            }

            throw new Exception($"Failed to get random posts: {await response.Content.ReadAsStringAsync()}");
        }

        public async Task<ProfilePictureResponse?> UploadProfilePictureAsync(IBrowserFile profilePicture)
        {
            return await _authService.UploadProfilePictureAsync(profilePicture);
        }

        public async Task<string?> GetProfilePictureAsync(string userId)
        {
            return await _authService.GetProfilePictureAsync(userId);
        }

        public async Task<UserProfile> GetUserProfileAsync(string userId)
        {
            await EnsureAuthHeader();
            var response = await _httpClient.GetAsync($"{BaseUrl}/user/{userId}/profile");

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<UserProfile>(jsonString, options);
            }

            throw new Exception($"Failed to get user profile: {await response.Content.ReadAsStringAsync()}");
        }

        public async Task<(List<Post> posts, long totalCount)> GetAllPostsPaginatedAsync(int page, int pageSize)
        {
            try
            {
                Console.WriteLine($"Fetching posts - Page: {page}, PageSize: {pageSize}");
                await EnsureAuthHeader();
                
                var url = $"{BaseUrl}/all?page={page}&pageSize={pageSize}";
                Console.WriteLine($"Making request to: {url}");
                
                var response = await _httpClient.GetAsync(url);
                Console.WriteLine($"Response status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Raw JSON response: {content}");

                    if (string.IsNullOrEmpty(content))
                    {
                        Console.WriteLine("Warning: Empty response content");
                        return (new List<Post>(), 0);
                    }

                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true,
                        WriteIndented = true
                    };

                    try
                    {
                        List<PostResponse> postResponses = null;
                        long total = 0;

                        // First try to deserialize as a simple list
                        try
                        {
                            var simpleList = JsonSerializer.Deserialize<List<PostResponse>>(content, options);
                            if (simpleList != null)
                            {
                                Console.WriteLine("Successfully deserialized as simple list");
                                postResponses = simpleList;
                                total = simpleList.Count; // Assuming total is the count of items in a simple list
                            }
                        }
                        catch (JsonException)
                        {
                            Console.WriteLine("Not a simple list, trying PaginatedResponse");
                        }

                        // If not a simple list or if we still need to try paginated response
                        if (postResponses == null)
                        {
                            var paginatedResponse = JsonSerializer.Deserialize<Models.PaginatedResponse<PostResponse>>(content, options);
                            if (paginatedResponse != null)
                            {
                                Console.WriteLine($"Successfully deserialized as PaginatedResponse with {paginatedResponse.Items?.Count ?? 0} items");
                                postResponses = paginatedResponse.Items;
                                total = paginatedResponse.TotalCount;
                            }
                            else
                            {
                                Console.WriteLine("Warning: Deserialized result is null (neither simple list nor PaginatedResponse)");
                                return (new List<Post>(), 0);
                            }
                        }

                        // Map PostResponse to Post and include ImageBase64
                        var posts = postResponses.Select(pr =>
                        {
                             if (pr?.Post == null)
                             {
                                 Console.WriteLine("Warning: Post object is null in PostResponse");
                                 return null;
                             }
                            pr.Post.ImageBase64 = pr.ImageBase64; // Copy ImageBase64 to the Post object
                            return pr.Post;
                        }).Where(p => p != null).ToList();
                        
                        Console.WriteLine($"Mapped {posts.Count} posts for frontend");

                        return (posts, total);

                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"JSON deserialization error: {ex.Message}");
                        Console.WriteLine($"JSON content: {content}");
                        Console.WriteLine($"Error path: {ex.Path}");
                        Console.WriteLine($"Line number: {ex.LineNumber}");
                        Console.WriteLine($"Byte position: {ex.BytePositionInLine}");
                        throw new Exception($"Failed to parse response: {ex.Message}", ex);
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Error response: {errorContent}");
                    throw new Exception($"Failed to load posts. Status: {response.StatusCode}, Content: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetAllPostsPaginatedAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner exception stack trace: {ex.InnerException.StackTrace}");
                }
                throw new Exception($"Error loading posts: {ex.Message}", ex);
            }
        }

        public async Task<(List<Post> Posts, long TotalCount)> GetUserPostsPaginatedAsync(string userId, int page, int pageSize)
        {
            await EnsureAuthHeader();
            var response = await _httpClient.GetAsync($"{BaseUrl}/user/{userId}?page={page}&pageSize={pageSize}");

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<PaginatedResponse<PostResponse>>(jsonString, options);
                
                if (result != null)
                {
                    var posts = result.Items.Select(pr =>
                    {
                        pr.Post.ImageBase64 = pr.ImageBase64;
                        return pr.Post;
                    }).ToList();
                    return (posts, result.TotalCount);
                }
            }
            return (new List<Post>(), 0);
        }

        public async Task<(List<Post> Posts, long TotalCount)> GetFollowedPostsPaginatedAsync(int page, int pageSize)
        {
            await EnsureAuthHeader();
            var response = await _httpClient.GetAsync($"{BaseUrl}/followed?page={page}&pageSize={pageSize}");

            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<PaginatedResponse<PostResponse>>(jsonString, options);
                
                if (result != null)
                {
                    var posts = result.Items.Select(pr =>
                    {
                        pr.Post.ImageBase64 = pr.ImageBase64;
                        return pr.Post;
                    }).ToList();
                    return (posts, result.TotalCount);
                }
            }
            return (new List<Post>(), 0);
        }

        public async Task<bool> UpdateBioAsync(string bio)
        {
            await EnsureAuthHeader();
            var content = new StringContent(JsonSerializer.Serialize(new { Bio = bio }), Encoding.UTF8, "application/json");
            var response = await _httpClient.PutAsync($"{BaseUrl}/bio", content);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<SearchResult>> SearchContentAsync(string query)
        {
            try
            {
                await EnsureAuthHeader();
                var url = $"{BaseUrl}/search?query={Uri.EscapeDataString(query)}";
                Console.WriteLine($"Making search request to: {url}");
                
                var response = await _httpClient.GetAsync(url);
                Console.WriteLine($"Search response status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Raw search response: {jsonString}");

                    var options = new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true,
                        WriteIndented = true
                    };

                    try
                    {
                        var results = JsonSerializer.Deserialize<List<SearchResult>>(jsonString, options);
                        Console.WriteLine($"Deserialized {results?.Count ?? 0} search results");
                        
                        if (results != null)
                        {
                            foreach (var result in results)
                            {
                                Console.WriteLine($"Search result - ID: {result.Id}, Title: {result.Title}, Score: {result.SimilarityScore}");
                            }
                        }
                        
                        return results ?? new List<SearchResult>();
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"JSON deserialization error: {ex.Message}");
                        Console.WriteLine($"Error path: {ex.Path}");
                        Console.WriteLine($"Line number: {ex.LineNumber}");
                        Console.WriteLine($"Byte position: {ex.BytePositionInLine}");
                        throw new Exception($"Failed to parse search results: {ex.Message}", ex);
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Search error response: {errorContent}");
                    throw new Exception($"Search failed. Status: {response.StatusCode}, Content: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SearchContentAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner exception stack trace: {ex.InnerException.StackTrace}");
                }
                throw;
            }
        }

        public async Task<CombinedSearchResult> PerformCombinedSearchAsync(string query)
        {
            try
            {
                await EnsureAuthHeader();
                var url = $"{BaseUrl}/search?query={Uri.EscapeDataString(query)}";
                Console.WriteLine($"Making combined search request to: {url}");
                
                var response = await _httpClient.GetAsync(url);
                Console.WriteLine($"Combined search response status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Raw combined search response: {jsonString}");

                    var options = new JsonSerializerOptions 
                    { 
                        PropertyNameCaseInsensitive = true,
                        WriteIndented = true
                    };

                    try
                    {
                        var result = JsonSerializer.Deserialize<CombinedSearchResult>(jsonString, options);
                        Console.WriteLine($"Deserialized combined search result. Users: {result?.Users?.Count ?? 0}, Posts: {result?.Posts?.Count ?? 0}");
                        
                        return result ?? new CombinedSearchResult();
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine($"JSON deserialization error for combined search: {ex.Message}");
                        Console.WriteLine($"JSON content: {jsonString}");
                        Console.WriteLine($"Error path: {ex.Path}");
                        Console.WriteLine($"Line number: {ex.LineNumber}");
                        Console.WriteLine($"Byte position: {ex.BytePositionInLine}");
                        throw new Exception($"Failed to parse combined search results: {ex.Message}", ex);
                    }
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"Combined search error response: {errorContent}");
                    throw new Exception($"Combined search failed. Status: {response.StatusCode}, Content: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in PerformCombinedSearchAsync: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner exception stack trace: {ex.InnerException.StackTrace}");
                }
                throw;
            }
        }
    }
}