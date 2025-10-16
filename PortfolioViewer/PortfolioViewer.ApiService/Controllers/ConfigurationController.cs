using GhostfolioSidekick.Configuration;
using Microsoft.AspNetCore.Mvc;

namespace GhostfolioSidekick.PortfolioViewer.ApiService.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class ConfigurationController(IApplicationSettings applicationSettings) : ControllerBase
	{
		[HttpGet("primary-currency")]
		public IActionResult GetPrimaryCurrency()
		{
			try
			{
				var primaryCurrencySymbol = applicationSettings.ConfigurationInstance?.Settings?.PrimaryCurrency;

				if (string.IsNullOrWhiteSpace(primaryCurrencySymbol))
				{
					primaryCurrencySymbol = "EUR"; // Default fallback
				}

				return Ok(new { PrimaryCurrency = primaryCurrencySymbol });
			}
			catch (Exception ex)
			{
				return StatusCode(500, new { message = "Failed to retrieve primary currency", error = ex.Message });
			}
		}
	}
}