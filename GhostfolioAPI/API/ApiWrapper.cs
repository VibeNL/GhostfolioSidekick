using CryptoExchange.Net.CommonObjects;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.GhostfolioAPI.API.Mapper;
using GhostfolioSidekick.GhostfolioAPI.Contract;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Symbols;
using KellermanSoftware.CompareNetObjects;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GhostfolioSidekick.GhostfolioAPI.API
{
	public class ApiWrapper(
			RestCall restCall,
			ILogger<ApiWrapper> logger,
			IApplicationSettings applicationSettings,
			ICurrencyExchange currencyExchange) : IApiWrapper
	{
		public async Task CreateAccount(Model.Accounts.Account account)
		{
			var o = new JObject
			{
				["name"] = account.Name,
				["currency"] = account.Balance.FirstOrDefault()?.Money.Currency.Symbol ?? Currency.EUR.ToString(),
				["comment"] = account.Comment,
				["platformId"] = null,
				["isExcluded"] = false,
				["balance"] = 0,
			};

			if (account.Platform != null)
			{
				var platforms = await GetPlatforms();
				var platform = platforms.SingleOrDefault(x => x.Name == account.Platform.Name);
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

		public async Task CreatePlatform(Model.Accounts.Platform platform)
		{
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

		public async Task<Model.Accounts.Account?> GetAccountByName(string name)
		{
			var accounts = await GetAllAccounts();
			var account = accounts.SingleOrDefault(x => string.Equals(x.Name, name, StringComparison.InvariantCultureIgnoreCase));
			if (account == null)
			{
				return null;
			}

			var platforms = await GetPlatforms();
			return ContractToModelMapper.MapAccount(
					account,
					platforms.SingleOrDefault(x => x.Id == account.PlatformId));
		}

		public async Task<Model.Accounts.Platform?> GetPlatformByName(string name)
		{
			var platforms = await GetPlatforms();
			var platform = platforms.SingleOrDefault(x => x.Name == name);

			if (platform == null)
			{
				return null;
			}

			return ContractToModelMapper.MapPlatform(platform);
		}

		public async Task<List<Model.Symbols.SymbolProfile>> GetSymbolProfile(string identifier, bool includeIndexes)
		{
			var content = await restCall.DoRestGet(
				$"api/v1/symbol/lookup?query={identifier.Trim()}&includeIndices={includeIndexes.ToString().ToLowerInvariant()}");
			if (content == null)
			{
				return [];
			}

			var symbolProfileList = JsonConvert.DeserializeObject<Contract.SymbolProfileList>(content);
			var assets = symbolProfileList!.Items.Select(ContractToModelMapper.MapSymbolProfile).ToList();
			return assets;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S3776:Cognitive Complexity of methods should not be too high", Justification = "<Pending>")]
		public async Task SyncAllActivities(List<Model.Activities.Activity> allActivities)
		{
			var content = await restCall.DoRestGet($"api/v1/order");
			var existingActivities = JsonConvert.DeserializeObject<ActivityList>(content!)!.Activities;

			// fixup
			foreach (var existingActivity in existingActivities)
			{
				existingActivity.FeeCurrency = existingActivity.FeeCurrency ?? existingActivity.SymbolProfile.Currency;
			}

			var symbols = await GetAllSymbolProfiles();
			var accounts = await GetAllAccounts();

			// Get new activities
			var newActivities = allActivities.Select(async activity =>
			{
				var symbolProfile = activity.Holding?.SymbolProfiles.SingleOrDefault(x => Datasource.IsGhostfolio(x.DataSource));
				var ghostfolioSymbolProfile = symbols.SingleOrDefault(x => x.Symbol == symbolProfile?.Symbol);

				// Create symbol if not found
				if (symbolProfile != null && ghostfolioSymbolProfile == null)
				{
					logger.LogWarning("Symbol not found {Symbol}, creating symbol", symbolProfile.Symbol);
					ghostfolioSymbolProfile = new Contract.SymbolProfile {
						AssetClass = symbolProfile.AssetClass.ToString(),
						AssetSubClass = symbolProfile.AssetSubClass?.ToString(),
						Currency = symbolProfile.Currency.Symbol,
						DataSource = Datasource.GHOSTFOLIO.ToString(),
						Name = symbolProfile.Name ?? symbolProfile.Symbol,
						Symbol = symbolProfile.Symbol,
						Sectors = symbolProfile.SectorWeights.Select(x => new Contract.Sector { Name = x.Name, Weight = x.Weight }).ToArray(),
						Countries = symbolProfile.CountryWeight.Select(x => new Contract.Country { Name = x.Name, Code = x.Code, Continent = x.Continent, Weight = x.Weight }).ToArray()	
					};
				}
				/*if (symbolProfile != null && ghostfolioSymbolProfile == null)
				{
					logger.LogWarning("Symbol not found {Symbol}, skipping activity", symbolProfile.Symbol);
					return null;
				}*/

				var account = accounts.SingleOrDefault(x => x.Name == activity.Account.Name);
				var convertedActivity = await ModelToContractMapper.ConvertToGhostfolioActivity(currencyExchange, ghostfolioSymbolProfile, activity, account);
				return convertedActivity;
			})
				.Where(x => !x.IsFaulted)
				.Select(x => x.Result)
				.Where(x => x != null && x.Type != ActivityType.IGNORE)
				.Select(x => x!)
				.Where(x => x.AccountId != null) // ignore when we have no account
				.ToList();

			var listA = Sortorder(existingActivities);
			var listB = Sortorder(newActivities.ToArray<Activity>());

			// loop all activities and compare
			for (var i = 0; i < Math.Min(listA.Count, listB.Count); i++)
			{
				var compareLogic = new CompareLogic()
				{
					Config = new ComparisonConfig
					{
						MaxDifferences = int.MaxValue,
						IgnoreObjectTypes = true,
						MembersToIgnore = [nameof(Activity.Id), nameof(Activity.ReferenceCode)],
						DecimalPrecision = 5
					}
				};
				var comparisonResult = compareLogic.Compare(listA[i], listB[i]);
				if (!comparisonResult.AreEqual)
				{
					await DeleteOrder(listA[i]);
					await WriteOrder(listB[i]);
				}
			}

			// Add new activities
			for (var i = Math.Min(listA.Count, listB.Count); i < listB.Count; i++)
			{
				await WriteOrder(listB[i]);
			}

			// Delete old activities
			for (var i = Math.Min(listA.Count, listB.Count); i < listA.Count; i++)
			{
				await DeleteOrder(listA[i]);
			}

			static List<Activity> Sortorder(Activity[] existingActivities)
			{
				return existingActivities
						.OrderBy(x => x.Date)
						.ThenBy(x => x.SymbolProfile.Symbol)
						.ThenBy(x => x.Comment)
						.ToList();
			}
		}

		public async Task UpdateAccount(Model.Accounts.Account account)
		{
			var accounts = await GetAllAccounts();
			var existingAccount = accounts.Single(x => string.Equals(x.Name, account.Name, StringComparison.InvariantCultureIgnoreCase));
			var content = await restCall.DoRestGet($"api/v1/account/{existingAccount.Id}/balances");

			var balanceList = JsonConvert.DeserializeObject<BalanceList>(content!);

			if (balanceList == null)
			{
				throw new NotSupportedException("Account not found");
			}

			// Delete all balances that are not in the new list
			foreach (var item in balanceList.Balances.Where(x => !account.Balance.Any(y => DateOnly.FromDateTime(x.Date) == y.Date)))
			{
				await restCall.DoRestDelete($"api/v1/account-balance/{item.Id}");
			}

			// Update all balances that are in the new list
			foreach (var newBalance in account.Balance)
			{
				var o = new JObject
				{
					["balance"] = newBalance.Money.Amount,
					["date"] = newBalance.Date.ToString("o"),
					["accountId"] = existingAccount.Id
				};
				var res = o.ToString();

				// check if balance already exists
				var existingBalance = balanceList.Balances.SingleOrDefault(x => DateOnly.FromDateTime(x.Date) == newBalance.Date);
				if (existingBalance != null)
				{
					if (Math.Round(existingBalance.Value, 10) == Math.Round(newBalance.Money.Amount, 10))
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

		private async Task<Contract.Account[]> GetAllAccounts()
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

			return accounts;
		}

		private async Task<List<Contract.Platform>> GetPlatforms()
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

			return rawPlatforms.ToList();
		}

		private async Task<List<Contract.SymbolProfile>> GetAllSymbolProfiles()
		{
			var content = await restCall.DoRestGet($"api/v1/admin/market-data/");

			if (content == null)
			{
				return [];
			}

			var market = JsonConvert.DeserializeObject<MarketDataList>(content);

			var profiles = new List<Contract.SymbolProfile>();
			foreach (var f in market?.MarketData
				.Where(x => !string.IsNullOrWhiteSpace(x.Symbol) && !string.IsNullOrWhiteSpace(x.DataSource))
				.ToList() ?? [])
			{
				content = await restCall.DoRestGet($"api/v1/admin/market-data/{f.DataSource}/{f.Symbol}");
				var data = JsonConvert.DeserializeObject<MarketDataListNoMarketData>(content!);
				profiles.Add(data!.AssetProfile);
			}

			return profiles;
		}

		private async Task CreateSymbol(Model.Symbols.SymbolProfile symbolProfile)
		{
			var o = new JObject
			{
				["symbol"] = symbolProfile.Symbol,
				["isin"] = symbolProfile.ISIN,
				["name"] = symbolProfile.Name,
				["comment"] = symbolProfile.Comment,
				["assetClass"] = symbolProfile.AssetClass.ToString(),
				["assetSubClass"] = symbolProfile.AssetSubClass?.ToString(),
				["currency"] = symbolProfile.Currency.Symbol,
				["datasource"] = symbolProfile.DataSource.ToString(),
			};
			var res = o.ToString();

			var r = await restCall.DoRestPost($"api/v1/admin/profile-data/{symbolProfile.DataSource}/{symbolProfile.Symbol}", res);
			if (!r.IsSuccessStatusCode)
			{
				throw new NotSupportedException($"Creation failed {symbolProfile.Symbol}");
			}

			logger.LogDebug("Created symbol {Symbol}", symbolProfile.Symbol);

			// Set name and assetClass (BUG / Quirk Ghostfolio?)
			////TODO await UpdateSymbol(symbolProfile);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1121:Assignments should not be made from within sub-expressions", Justification = "Cleaner")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2629:Logging templates should be constant", Justification = "<Pending>")]
		private async Task WriteOrder(Activity activity)
		{
			if (activity.Type == ActivityType.IGNORE)
			{
				logger.LogTrace("Skipping ignore transaction {Date} {Symbol} {Quantity} {Type}", activity.Date.ToInvariantString(), activity.SymbolProfile?.Symbol, activity.Quantity, activity.Type);

				return;
			}

			var url = $"api/v1/order";
			await restCall.DoRestPost(url, await ConvertToBody(activity));

			logger.LogDebug("Added transaction {Date} {Symbol} {Quantity} {Type}", activity.Date.ToInvariantString(), activity.SymbolProfile?.Symbol, activity.Quantity, activity.Type);
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2629:Logging templates should be constant", Justification = "<Pending>")]
		private async Task DeleteOrder(Contract.Activity activity)
		{
			if (string.IsNullOrWhiteSpace(activity.Id))
			{
				throw new NotSupportedException($"Deletion failed, no Id");
			}

			await restCall.DoRestDelete($"api/v1/order/{activity.Id}");
			logger.LogDebug("Deleted transaction {Date} {Symbol} {Type}", activity.Date.ToInvariantString(), activity.SymbolProfile?.Symbol, activity.Type);
		}

		private static Task<string> ConvertToBody(Contract.Activity activity)
		{
			var o = new JObject
			{
				["accountId"] = activity.AccountId,
				["comment"] = activity.Comment,
				["currency"] = activity.SymbolProfile?.Currency,
				["dataSource"] = activity.SymbolProfile?.DataSource,
				["date"] = activity.Date.ToString("o"),
				["fee"] = activity.Fee,
				["quantity"] = activity.Quantity,
				["symbol"] = activity.SymbolProfile?.Symbol,
				["type"] = activity.Type.ToString(),
				["unitPrice"] = activity.UnitPrice
			};
			var res = o.ToString();
			return Task.FromResult(res);
		}

		private async Task<bool> CreateManualSymbol(Model.Symbols.SymbolProfile symbolProfile)
		{
			var symbolConfigurations = applicationSettings.ConfigurationInstance.Symbols;
			var symbolConfiguration = (symbolConfigurations ?? []).SingleOrDefault(x => x.Symbol == symbolProfile.Symbol);

			if (symbolConfiguration == null)
			{
				return false;
			}

			var manualSymbolConfiguration = symbolConfiguration.ManualSymbolConfiguration;
			if (manualSymbolConfiguration == null)
			{
				return false;
			}

			var subClass = EnumMapper.ParseAssetSubClass(manualSymbolConfiguration.AssetSubClass);
			Model.Activities.AssetSubClass[]? expectedAssetSubClass = subClass != null ? [subClass.Value] : null;

			await CreateSymbol(new Model.Symbols.SymbolProfile(
				symbolConfiguration.Symbol,
				manualSymbolConfiguration.Name,
				[symbolConfiguration.Symbol, manualSymbolConfiguration.Name],
				new Currency(manualSymbolConfiguration.Currency),
				Datasource.MANUAL,
				EnumMapper.ParseAssetClass(manualSymbolConfiguration.AssetClass),
				EnumMapper.ParseAssetSubClass(manualSymbolConfiguration.AssetSubClass),
				manualSymbolConfiguration.Countries.Select(x => new Model.Symbols.CountryWeight(x.Name, x.Code, x.Continent, x.Weight)).ToArray(),
				manualSymbolConfiguration.Sectors.Select(x => new Model.Symbols.SectorWeight(x.Name, x.Weight)).ToArray())
			{
				ISIN = manualSymbolConfiguration.ISIN
			}
			);

			////// Set scraper
			////if (symbol.ScraperConfiguration.Url != manualSymbolConfiguration.ScraperConfiguration?.Url ||
			////	symbol.ScraperConfiguration.Selector != manualSymbolConfiguration.ScraperConfiguration?.Selector ||
			////	symbol.ScraperConfiguration.Locale != manualSymbolConfiguration.ScraperConfiguration?.Locale
			////	)
			////{
			////	symbol.ScraperConfiguration.Url = manualSymbolConfiguration.ScraperConfiguration?.Url;
			////	symbol.ScraperConfiguration.Selector = manualSymbolConfiguration.ScraperConfiguration?.Selector;
			////	symbol.ScraperConfiguration.Locale = manualSymbolConfiguration.ScraperConfiguration?.Locale;
			////	await marketDataService.UpdateSymbol(symbol);
			////}

			////// Set countries, TODO: check all properties
			////var countries = manualSymbolConfiguration.Countries;
			////if (countries != null && !countries.Select(x => x.Code).SequenceEqual(symbol.Countries.Select(x => x.Code)))
			////{
			////	symbol.Countries = countries.Select(x => new Model.Symbols.Country(x.Name, x.Code, x.Continent, x.Weight));
			////	await marketDataService.UpdateSymbol(symbol);
			////}

			////// Set sectors, TODO: check all properties
			////var sectors = manualSymbolConfiguration.Sectors;
			////if (sectors != null && !sectors.Select(x => x.Name).SequenceEqual(symbol.Sectors.Select(x => x.Name)))
			////{
			////	symbol.Sectors = sectors.Select(x => new Model.Symbols.Sector(x.Name, x.Weight));
			////	await marketDataService.UpdateSymbol(symbol);
			////}

			return true;
		}
	}
}
