using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.Database.Repository
{
	public class AccountRepository(DatabaseContext databaseContext) : IAccountRepository
	{
		public Task AddAccount(Account account)
		{
			return databaseContext.Accounts.AddAsync(account).AsTask();
		}

		public Task AddPlatform(Platform platform)
		{
			return databaseContext.Platforms.AddAsync(platform).AsTask();
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
