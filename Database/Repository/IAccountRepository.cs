using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.Database.Repository
{
	public interface IAccountRepository
	{
		Task AddAccount(Account account);
		Task AddPlatform(Platform platform);
		Task<Account?> GetAccountByName(string accountName);
		Task<List<Account>> GetAllAccounts();
		Task<Platform?> GetPlatformByName(string name);
	}
}
