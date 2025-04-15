using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace PortfolioViewer.WASM.Services
{
    public class ChatService
    {
        private readonly HttpClient _httpClient;

        public ChatService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<string> SendMessage(string userMessage)
        {
            var response = await _httpClient.PostAsJsonAsync("/api/chatbot", new { message = userMessage });
            response.EnsureSuccessStatusCode();

            var botResponse = await response.Content.ReadFromJsonAsync<string>();
            return botResponse ?? string.Empty;
        }
    }
}
