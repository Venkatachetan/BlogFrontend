
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace BlogFrontend.Services
{
    public class AIContentService
    {
        private readonly HttpClient _httpClient;

        public AIContentService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> GenerateContentAsync(string title)
        {
            var request = new { Title = title };
            var response = await _httpClient.PostAsJsonAsync("api/ai-content/generate", request);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<GenerateContentResponse>();
            return result?.GeneratedContent;
        }
    }

    public class GenerateContentResponse
    {
        public string Title { get; set; }
        public string GeneratedContent { get; set; }
    }
}