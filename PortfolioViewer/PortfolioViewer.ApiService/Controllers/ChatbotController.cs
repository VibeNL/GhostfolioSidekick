using Microsoft.AspNetCore.Mvc;

namespace PortfolioViewer.ApiService.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ChatbotController : ControllerBase
    {
        [HttpPost]
        public IActionResult Post([FromBody] ChatMessage message)
        {
            var responseMessage = new ChatMessage
            {
                Content = $"Echo: {message.Content}"
            };

            return Ok(responseMessage);
        }
    }

    public class ChatMessage
    {
        public string Content { get; set; }
    }
}
