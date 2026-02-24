using GhostfolioSidekick.PortfolioViewer.Common;
using Microsoft.AspNetCore.Mvc;

namespace GhostfolioSidekick.PortfolioViewer.ApiService.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class VersionController : ControllerBase
	{
		[HttpGet]
		public IActionResult GetVersion()
		{
			return Ok(new { version = VersionInfo.Version });
		}
	}
}
