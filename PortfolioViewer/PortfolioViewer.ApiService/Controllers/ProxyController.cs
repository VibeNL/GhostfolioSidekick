using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Threading.Tasks;

namespace GhostfolioSidekick.PortfolioViewer.ApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProxyController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        public ProxyController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient();
        }

        [HttpGet("fetch")]
        public async Task<IActionResult> Fetch([FromQuery] string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return BadRequest("URL is required.");
            try
            {
                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    return StatusCode((int)response.StatusCode);
                var content = await response.Content.ReadAsStringAsync();
                return Ok(content);
            }
            catch
            {
                return StatusCode(500, "Failed to fetch content.");
            }
        }
    }
}
