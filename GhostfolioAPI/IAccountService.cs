﻿
using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.GhostfolioAPI
{
	public interface IAccountService
	{
		Task CreateAccount(Account account);

		Task CreatePlatform(Platform platform);

		Task DeleteAccount(string name);

		Task<Account?> GetAccountByName(string name);

		Task<Account[]> GetAllAccounts();

		Task<Platform?> GetPlatformByName(string name);

		Task UpdateBalance(Account existingAccount, Balance newBalance);
	}
}
