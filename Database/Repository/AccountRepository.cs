using GhostfolioSidekick.Model.Accounts;
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

		public Task<Account?> GetAccountByName(string accountName)
		{
			return databaseContext.Accounts.SingleOrDefaultAsync(x => x.Name == accountName);
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
