﻿using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.GhostfolioAPI.API
{
	public interface IApiWrapper
	{
		Task<Account?> GetAccountByName(string name);
		Task<Platform?> GetPlatformByName(string name);
				
		Task<List<SymbolProfile>> GetSymbolProfile(string identifier, bool includeIndexes);

		Task CreatePlatform(Platform platform);
		Task CreateAccount(Account account);
		Task UpdateAccount(Account account);
		Task SyncAllActivities(List<Activity> allActivities);
	}
}