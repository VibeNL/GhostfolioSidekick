using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.Database.Repository
{
	public class AccountRepository(DatabaseContext databaseContext) : IAccountRepository
	{
		public async Task AddAccount(Account account)
		{
			await databaseContext.Accounts.AddAsync(account).AsTask();
			await databaseContext.SaveChangesAsync();
		}

		public async Task AddPlatform(Platform platform)
		{
			await databaseContext.Platforms.AddAsync(platform).AsTask();
			await databaseContext.SaveChangesAsync();
		}

		public Task<List<Account>> GetAllAccounts()
		{
			return databaseContext.Accounts.ToListAsync();
		}

		public Task<Platform?> GetPlatformByName(string name)
		{
			return databaseContext.Platforms.SingleOrDefaultAsync(x => x.Name == name);
		}
	}
}
