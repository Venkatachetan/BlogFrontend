using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BlogFrontend.Models;
using Microsoft.AspNetCore.Components.Forms;

namespace BlogFrontend.Services
{
    public interface IBlogService
    {
        Task<List<Post>> GetAllPostsAsync();
        Task<List<Post>> GetUserPostsAsync(string userId);
        Task<Post> GetPostByIdAsync(string postId);
        Task<Post> CreatePostAsync(string title, string content, List<string> tags, IBrowserFile image);
        Task<Post> LikePostAsync(string postId);
        Task<Post> UnlikePostAsync(string postId);
        Task<bool> AddCommentAsync(string postId, string text);
        Task<bool> DeletePostAsync(string postId);
        Task<Post> UpdatePostAsync(string postId, string title, string content, IBrowserFile image, List<string> tags);
        Task<bool> DeleteCommentAsync(string postId, string commentId);
    }

    public class BlogService : IBlogService
    {
        private readonly HttpClient _httpClient;
        private readonly IAuthService _authService;
        private const string BaseUrl = "api/blog";

        public BlogService(HttpClient httpClient, IAuthService authService)
        {
            _httpClient = httpClient;
            _authService = authService;
        }

        private async Task EnsureAuthHeader()
        {
            var token = await _authService.GetTokenAsync();
            if (!string.IsNullOrEmpty(token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
            else
            {
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
            formData.Add(new StringContent(content), "Content"); // Content now includes HTML for rich text formatting

            if (tags != null && tags.Any())
            {
                foreach (var tag in tags)
                {
                    formData.Add(new StringContent(tag), "Tags");
                }
            }

            if (image != null)
            {
                var imageContent = new StreamContent(image.OpenReadStream(maxAllowedSize: 10485760)); // 10MB max
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

        public async Task<Post> LikePostAsync(string postId)
        {
            await EnsureAuthHeader();
            var response = await _httpClient.PostAsync($"{BaseUrl}/like/{postId}", null);
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<LikeResponse>(jsonString, options);
                return new Post
                {
                    Id = postId,
                    Likes = result.TotalLikes,
                    LikedBy = result.Likers.Select(l => new Like { UserId = l.UserId, UserName = l.UserName, LikedAt = l.LikedAt }).ToList()
                };
            }
            throw new Exception($"Failed to like post: {await response.Content.ReadAsStringAsync()}");
        }

        public async Task<Post> UnlikePostAsync(string postId)
        {
            await EnsureAuthHeader();
            var response = await _httpClient.PostAsync($"{BaseUrl}/unlike/{postId}", null);
            if (response.IsSuccessStatusCode)
            {
                var jsonString = await response.Content.ReadAsStringAsync();
                var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var result = JsonSerializer.Deserialize<UnlikeResponse>(jsonString, options);
                return new Post
                {
                    Id = postId,
                    Likes = result.TotalLikes
                };
            }
            throw new Exception($"Failed to unlike post: {await response.Content.ReadAsStringAsync()}");
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
            formData.Add(new StringContent(content), "Content"); // Content includes HTML for bold, headings, fonts, alignments

            if (tags != null && tags.Any())
            {
                foreach (var tag in tags)
                {
                    formData.Add(new StringContent(tag), "Tags");
                }
            }

            if (image != null)
            {
                var imageContent = new StreamContent(image.OpenReadStream(maxAllowedSize: 10485760)); // 10MB max
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
                    case 400: // BadRequest
                        throw new Exception($"Invalid request: {errorResponse?.Message ?? "Bad request"}");
                    case 401: // Unauthorized
                        throw new Exception($"Unauthorized: {errorResponse?.Message ?? "Authentication required"}");
                    case 403: // Forbidden
                        throw new Exception($"Forbidden: {errorResponse?.Message ?? "You lack permission to update this post"}");
                    case 404: // NotFound
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
    }

    // Response models
    public class PostResponse
    {
        public Post Post { get; set; }
        public string ImageBase64 { get; set; }
    }

    public class LikeResponse
    {
        public string Message { get; set; }
        public string LikedBy { get; set; }
        public int TotalLikes { get; set; }
        public List<LikeInfo> Likers { get; set; }
    }

    public class UnlikeResponse
    {
        public string Message { get; set; }
        public int TotalLikes { get; set; }
    }

    public class LikeInfo
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public DateTime LikedAt { get; set; }
    }

    public class ErrorResponse
    {
        public string Message { get; set; }
    }
}