using GhostfolioSidekick.Model.Accounts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
