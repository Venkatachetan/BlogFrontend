//using Microsoft.JSInterop;
//using System;
//using System.Net.Http;
//using System.Threading.Tasks;

//namespace BlogFrontend.Services
//{
//    public class TextReaderService
//    {
//        private readonly HttpClient _httpClient;
//        private readonly IJSRuntime _jsRuntime;

//        public TextReaderService(HttpClient httpClient, IJSRuntime jsRuntime)
//        {
//            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
//            _jsRuntime = jsRuntime ?? throw new ArgumentNullException(nameof(jsRuntime));
//        }

//        public async Task<byte[]> GetPostAudioAsync(string postId)
//        {
//            try
//            {
//                // Use GetByteArrayAsync to directly fetch the response as bytes
//                return await _httpClient.GetByteArrayAsync($"api/textreader/read/{postId}");
//            }
//            catch (HttpRequestException ex)
//            {
//                throw new Exception($"Failed to fetch audio for post {postId}: {ex.Message}", ex);
//            }
//        }

//        public async Task PlayAudio(byte[] audioBytes, double startPosition = 0)
//        {
//            var base64String = Convert.ToBase64String(audioBytes);
//            var audioUrl = $"data:audio/wav;base64,{base64String}";
//            await _jsRuntime.InvokeVoidAsync("playAudio", audioUrl, startPosition);
//        }

//        public async Task<double> PauseAudio()
//        {
//            return await _jsRuntime.InvokeAsync<double>("pauseAudio");
//        }
//    }
//}