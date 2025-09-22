//using Microsoft.AspNetCore.Mvc;
//using GhostfolioSidekick.Database;
//using GhostfolioSidekick.PortfolioViewer.Common.SQL;

//namespace GhostfolioSidekick.PortfolioViewer.ApiService.Controllers
//{
//	[Route("api/[controller]")]
//	[ApiController]
//	public class SyncController : ControllerBase
//	{
//		private readonly DatabaseContext _context;

//		public SyncController(DatabaseContext context)
//		{
//			_context = context;
//		}

//		[HttpGet("{entity}")]
//		public async Task<IActionResult> GetEntityData(string entity, [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
//		{
//			if (page <= 0 || pageSize <= 0)
//			{
//				return BadRequest(new { Error = "Page and pageSize must be greater than 0." });
//			}

//			try
//			{
//				var result = await RawQuery.ReadTable(_context, entity, page, pageSize);

//				// Return the result as JSON
//				return Ok(result);
//			}
//			catch (Exception ex)
//			{
//				// Handle exceptions (e.g., invalid table name)
//				return BadRequest(new { Error = ex.Message });
//			}
//		}

//	}
//}