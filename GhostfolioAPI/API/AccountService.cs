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
		private readonly ApplicationSettings applicationSettings;
		private readonly RestCall restCall;
		private readonly ILogger<AccountService> logger;

		public AccountService(
			ApplicationSettings applicationSettings,
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
			var platform = await GetPlatformByName(account.Name);

			var o = new JObject
			{
				["name"] = account.Name,
				["currency"] = account.Balance.Money.Currency.Symbol,
				["comment"] = account.Comment,
				["platformId"] = platform?.Id,
				["isExcluded"] = false,
				["balance"] = 0,
			};
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

		public Task<Model.Accounts.Platform> GetPlatformByName(string name)
		{
			throw new NotImplementedException();
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
	}
}
