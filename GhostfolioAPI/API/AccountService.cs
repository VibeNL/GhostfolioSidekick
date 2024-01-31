using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.GhostfolioAPI.API.Mapper;
using GhostfolioSidekick.GhostfolioAPI.Contract;
using GhostfolioSidekick.Model.Accounts;
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
			this.restCall = restCall ?? throw new ArgumentNullException(nameof(restCall));
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

			logger.LogInformation($"Created platform {platform.Name}");
		}

		public async Task CreateAccount(Model.Accounts.Account account)
		{
			var o = new JObject
			{
				["name"] = account.Name,
				["currency"] = account.Balance.Money.Currency.Symbol,
				["comment"] = account.Comment,

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

			logger.LogInformation($"Created account {account.Name}");
		}

		public async Task<Model.Accounts.Account> GetAccountByName(string name)
		{
			var content = await restCall.DoRestGet($"api/v1/account", CacheDuration.None());
			if (content == null)
			{
				throw new NotSupportedException();
			}

			var rawAccounts = JsonConvert.DeserializeObject<AccountList>(content);
			var rawAccount = rawAccounts!.Accounts.SingleOrDefault(x => string.Equals(x.Name, name, StringComparison.InvariantCultureIgnoreCase));

			if (rawAccount == null)
			{
				throw new NotSupportedException();
			}

			var platforms = await GetPlatforms();
			var platform = platforms.SingleOrDefault(x => x.Id == rawAccount.PlatformId);
			return ContractToModelMapper.MapAccount(rawAccount, platform);
		}

		public async Task<Model.Accounts.Platform> GetPlatformByName(string name)
		{
			var platforms = await GetPlatforms();
			return platforms.Single(x => x.Name == name);
		}

		public async Task<List<Model.Accounts.Platform>> GetPlatforms()
		{
			var content = await restCall.DoRestGet($"api/v1/platform", CacheDuration.None());

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

		public async Task UpdateBalance(Model.Accounts.Account existingAccount, Balance newBalance)
		{
			var content = await restCall.DoRestGet($"api/v1/account", CacheDuration.Short());

			var rawAccounts = JsonConvert.DeserializeObject<AccountList>(content!);
			var rawAccount = rawAccounts?.Accounts?.SingleOrDefault(x => string.Equals(x.Id, existingAccount.Id, StringComparison.InvariantCultureIgnoreCase));

			if (rawAccount == null)
			{
				throw new NotSupportedException("Account not found");
			}

			if (Math.Round(rawAccount.Balance, 10) == Math.Round(newBalance.Money.Amount, 10))
			{
				return;
			}

			var o = new JObject();
			o["balance"] = newBalance.Money.Amount;
			o["comment"] = rawAccount.Comment;
			o["currency"] = newBalance.Money.Currency.Symbol;
			o["id"] = rawAccount.Id;
			o["isExcluded"] = rawAccount.IsExcluded;
			o["name"] = rawAccount.Name;
			o["platformId"] = rawAccount.PlatformId;
			var res = o.ToString();

			await restCall.DoRestPut($"api/v1/account/{existingAccount.Id}", res);
		}
	}
}
