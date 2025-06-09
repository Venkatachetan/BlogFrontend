using System.Net.Http.Json;
using Markdig;
using BlogFrontend.Models;
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
            try
            {
                var request = new { Title = title };
                var response = await _httpClient.PostAsJsonAsync("api/ai-content/generate", request);
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"AI Content Generation failed: {response.StatusCode} - {errorContent}");
                    throw new Exception($"Failed to generate content: {response.StatusCode} - {errorContent}");
                }

                var result = await response.Content.ReadFromJsonAsync<GenerateContentResponse>();
                if (result == null)
                {
                    throw new Exception("Failed to deserialize AI content response");
                }

                string markdownContent = result.GeneratedContent;
                if (string.IsNullOrEmpty(markdownContent))
                {
                    throw new Exception("Generated content is empty");
                }

                var pipeline = new MarkdownPipelineBuilder()
                    .UseAdvancedExtensions()
                    .Build();
                return Markdig.Markdown.ToHtml(markdownContent, pipeline);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GenerateContentAsync: {ex.Message}");
                throw new Exception($"Failed to generate content: {ex.Message}", ex);
            }
        }
    }

}