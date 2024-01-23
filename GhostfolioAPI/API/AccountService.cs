using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.GhostfolioAPI.API.Mapper;
using GhostfolioSidekick.GhostfolioAPI.Contract;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace GhostfolioSidekick.GhostfolioAPI.API
{
	public class AccountService : IAccountService
	{
		private readonly RestCall restCall;

		public AccountService(RestCall restCall)
		{
			this.restCall = restCall ?? throw new ArgumentNullException(nameof(restCall));
		}

		public Task CreateAccount(Model.Accounts.Account account)
		{
			throw new NotImplementedException();
		}

		public Task CreatePlatform(Model.Accounts.Platform platform)
		{
			throw new NotImplementedException();
		}

		public async Task<Model.Accounts.Account> GetAccountByName(string name)
		{
			var content = await restCall.DoRestGet($"api/v1/account", CacheDuration.Short());
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
