using Microsoft.AspNetCore.Mvc;
using GhostfolioSidekick.Database;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;

namespace GhostfolioSidekick.PortfolioViewer.ApiService.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SyncController : ControllerBase
    {
        private readonly DatabaseContext _context;

        public SyncController(DatabaseContext context)
        {
            _context = context;
        }

        [HttpGet("{entity}")]
        public async Task<IActionResult> GetEntityData(string entity)
        {
            var dbSet = _context.GetSyncableSet(entity);
            if (dbSet == null)
            {
                return NotFound();
            }

            var data = await dbSet.ToListAsync();
            return Ok(data);
        }

        [HttpGet("{entity}/hash")]
        public async Task<IActionResult> GetEntityHash(string entity)
        {
            var dbSet = _context.GetSyncableSet(entity);
            if (dbSet == null)
            {
                return NotFound();
            }

            var data = await dbSet.ToListAsync();
            var jsonData = JsonConvert.SerializeObject(data);
            using var sha256 = SHA256.Create();
            var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(jsonData));
            var hash = BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            return Ok(hash);
        }
    }
}
