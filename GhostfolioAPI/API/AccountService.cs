using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.GhostfolioAPI.API.Mapper;
using GhostfolioSidekick.GhostfolioAPI.Contract;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GhostfolioSidekick.GhostfolioAPI.API
{
	public class AccountService : IAccountService
	{
		private readonly IApplicationSettings applicationSettings;
		private readonly RestCall restCall;
		private readonly ILogger<AccountService> logger;

		public AccountService(
			IApplicationSettings applicationSettings,
			RestCall restCall,
			ILogger<AccountService> logger)
		{
			this.applicationSettings = applicationSettings;
			this.restCall = restCall;
			this.logger = logger;
		}

		public async Task CreatePlatform(Model.Accounts.Platform platform)
		{
			if (!applicationSettings.AllowAdminCalls)
			{
				return;
			}

			var o = new JObject
			{
				["name"] = platform.Name,
				["url"] = platform.Url
			};
			var res = o.ToString();

			var r = await restCall.DoRestPost($"api/v1/platform/", res);
			if (!r.IsSuccessStatusCode)
			{
				throw new NotSupportedException($"Creation failed {platform.Name}");
			}

			logger.LogDebug("Created platform {Name}", platform.Name);
		}

		public async Task CreateAccount(Model.Accounts.Account account)
		{
			var o = new JObject
			{
				["name"] = account.Name,
				["currency"] = account.Balance.Money.Currency.Symbol,
				["comment"] = account.Comment,
				["platformId"] = null,
				["isExcluded"] = false,
				["balance"] = 0,
			};

			if (account.Platform != null)
			{
				var platform = await GetPlatformByName(account.Platform.Name);
				o["platformId"] = platform?.Id;
			}

			var res = o.ToString();

			var r = await restCall.DoRestPost($"api/v1/account/", res);
			if (!r.IsSuccessStatusCode)
			{
				throw new NotSupportedException($"Creation failed {account.Name}");
			}

			logger.LogDebug("Created account {Name}", account.Name);
		}

		public async Task<Model.Accounts.Account?> GetAccountByName(string name)
		{
			var accounts = await GetAllAccounts();
			return accounts.SingleOrDefault(x => string.Equals(x.Name, name, StringComparison.InvariantCultureIgnoreCase));
		}

		public async Task<Model.Accounts.Account[]> GetAllAccounts()
		{
			var content = await restCall.DoRestGet($"api/v1/account");

			if (content == null)
			{
				throw new NotSupportedException();
			}

			var rawAccounts = JsonConvert.DeserializeObject<AccountList>(content);
			var accounts = rawAccounts?.Accounts;

			if (accounts == null)
			{
				return [];
			}

			var platforms = await GetPlatforms();
			var mappedAccounts = accounts.Select(x => ContractToModelMapper.MapAccount(x, platforms.SingleOrDefault(p => p.Id == x.PlatformId))).ToArray();

			return mappedAccounts;
		}

		public async Task<Model.Accounts.Platform?> GetPlatformByName(string name)
		{
			var platforms = await GetPlatforms();
			return platforms.SingleOrDefault(x => x.Name == name);
		}

		public async Task<List<Model.Accounts.Platform>> GetPlatforms()
		{
			var content = await restCall.DoRestGet($"api/v1/platform");

			if (content == null)
			{
				return [];
			}

			var rawPlatforms = JsonConvert.DeserializeObject<Contract.Platform[]>(content);
			if (rawPlatforms == null)
			{
				throw new NotSupportedException();
			}

			return rawPlatforms.Select(ContractToModelMapper.MapPlatform).ToList();
		}

		public async Task SetBalances(Model.Accounts.Account existingAccount, Dictionary<DateOnly, Model.Accounts.Balance> balances)
		{
			var content = await restCall.DoRestGet($"api/v1/account/{existingAccount.Id}/balances");

			var balanceList = JsonConvert.DeserializeObject<BalanceList>(content!);

			if (balanceList == null)
			{
				throw new NotSupportedException("Account not found");
			}

			// Delete all balances that are not in the new list
			foreach (var item in balanceList.Balances.Where(x => !balances.Any(y => DateOnly.FromDateTime(x.Date) == y.Key)))
			{
				await restCall.DoRestDelete($"api/v1/account-balance/{item.Id}");
			}

			// Update all balances that are in the new list
			foreach (var newBalance in balances)
			{
				var o = new JObject
				{
					["balance"] = newBalance.Value.Money.Amount,
					["date"] = newBalance.Key.ToString("o"),
					["accountId"] = existingAccount.Id
				};
				var res = o.ToString();

				// check if balance already exists
				var existingBalance = balanceList.Balances.SingleOrDefault(x => DateOnly.FromDateTime(x.Date) == newBalance.Key);
				if (existingBalance != null)
				{
					if (Math.Round(existingBalance.Value, 10) == Math.Round(newBalance.Value.Money.Amount, 10))
					{
						continue;
					}

					await restCall.DoRestDelete($"api/v1/account-balance/{existingBalance.Id}");
					await restCall.DoRestPost($"api/v1/account-balance/", res);
				}
				else
				{
					await restCall.DoRestPost($"api/v1/account-balance/", res);
				}
			}
		}

		public async Task DeleteAccount(string name)
		{
			var account = await GetAccountByName(name);

			if (account != null)
			{
				await restCall.DoRestDelete($"api/v1/account/{account.Id}");
			}
		}
	}
}
