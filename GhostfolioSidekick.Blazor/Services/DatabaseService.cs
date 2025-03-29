using GhostfolioSidekick.Blazor.Models;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.Blazor.Services
{
    public class DatabaseService
    {
        private readonly DatabaseContext _context;

        public DatabaseService(DatabaseContext context)
        {
            _context = context;
        }

        public async Task<List<Platform>> GetPlatformsAsync()
        {
            return await _context.Platforms.ToListAsync();
        }

        public async Task<List<Account>> GetAccountsAsync()
        {
            return await _context.Accounts.ToListAsync();
        }

        public async Task<List<SymbolProfile>> GetSymbolProfilesAsync()
        {
            return await _context.SymbolProfiles.ToListAsync();
        }

        public async Task<List<Activity>> GetActivitiesAsync()
        {
            return await _context.Activities.ToListAsync();
        }
    }
}
