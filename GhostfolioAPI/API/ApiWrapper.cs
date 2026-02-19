using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.GhostfolioAPI.API.Compare;
using GhostfolioSidekick.GhostfolioAPI.API.Mapper;
using GhostfolioSidekick.GhostfolioAPI.Contract;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GhostfolioSidekick.GhostfolioAPI.API
{
	public class ApiWrapper(
			RestCall restCall,
			ILogger<ApiWrapper> logger,
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

			var symbolProfileList = JsonConvert.DeserializeObject<SymbolProfileList>(content);
			var assets = symbolProfileList!.Items.Select(ContractToModelMapper.MapSymbolProfile).ToList();
			return assets;
		}

		public async Task<List<Model.Activities.Activity>> GetActivitiesByAccount(Model.Accounts.Account account)
		{
			var rawAccounts = await GetAllAccounts();
			var rawAccount = rawAccounts.SingleOrDefault(x => string.Equals(x.Name, account.Name, StringComparison.InvariantCultureIgnoreCase)) ?? throw new NotSupportedException("Account not found");
			var content = await restCall.DoRestGet($"api/v1/order");
			var existingActivities = JsonConvert.DeserializeObject<ActivityList>(content!)!.Activities.ToList();

			existingActivities = [.. existingActivities.Where(x => x.AccountId == rawAccount.Id)];

			if (existingActivities.Count == 0)
			{
				return [];
			}

			var symbols = await GetAllSymbolProfiles();
			return [.. existingActivities.Select(x => ContractToModelMapper.MapActivity(account, symbols, x))];
		}

		public async Task SyncAllActivities(List<Model.Activities.Activity> allActivities)
		{
			var content = await restCall.DoRestGet($"api/v1/order");
			var existingActivities = JsonConvert.DeserializeObject<ActivityList>(content!)!.Activities.ToList();

			// fixup
			foreach (var existingActivity in existingActivities)
			{
				existingActivity.FeeCurrency ??= existingActivity.SymbolProfile.Currency;
			}

			var accounts = await GetAllAccounts();

			// Filter out existing activities that are not in the new list
			var accountFromActivities = allActivities.Select(x => x.Account.Name).Distinct().ToList();
			var accountIds = accountFromActivities.Select(x => accounts.SingleOrDefault(y => y.Name == x)?.Id).Where(x => x != null).Select(x => x!).ToList();
			existingActivities = [.. existingActivities.Where(x => accountIds.Contains(x.AccountId!))];

			// Get new activities
			var newActivities = allActivities.Select(activity =>
			{
				var symbolProfile = activity
					.Holding?
					.SymbolProfiles
					.Where(x => Datasource.IsGhostfolio(x.DataSource))
					.OrderBy(x => SortOnDataSource(activity, x)) // Sort on the datasource
					.FirstOrDefault();
				Contract.SymbolProfile? ghostfolioSymbolProfile = null;

				if (symbolProfile != null)
				{
					ghostfolioSymbolProfile = new Contract.SymbolProfile
					{
						AssetClass = symbolProfile.AssetClass.ToString(),
						AssetSubClass = symbolProfile.AssetSubClass?.ToString(),
						Currency = symbolProfile.Currency.Symbol,
						DataSource = Datasource.GetUnderlyingDataSource(symbolProfile.DataSource).ToString(),
						Name = symbolProfile.Name ?? symbolProfile.Symbol,
						Symbol = symbolProfile.Symbol,
						Sectors = symbolProfile.SectorWeights?.Select(x => new Sector { Name = x.Name, Weight = x.Weight }).ToArray() ?? [],
						Countries = symbolProfile.CountryWeight?.Select(x => new Country { Name = x.Name, Code = x.Code, Continent = x.Continent, Weight = x.Weight }).ToArray() ?? []
					};
				}

				var account = accounts.SingleOrDefault(x => x.Name == activity.Account.Name);
				var convertedActivity = ModelToContractMapper.ConvertToGhostfolioActivity(currencyExchange, ghostfolioSymbolProfile, activity, account).Result;
				return convertedActivity;
			})
			.Where(x => x != null && x.Type != ActivityType.IGNORE)
			.Select(x => x!)
			.Where(x => x.AccountId != null) // ignore when we have no account
			.ToList();

			var mergeOrders = (await MergeActivities.Merge(existingActivities, newActivities))
				.Where(x => x.Operation != Operation.Duplicate)
				.OrderBy(x => x.Order1.Date)
				.ToList();

			logger.LogDebug("Applying changes");
			foreach (var item in mergeOrders)
			{
				try
				{
					switch (item.Operation)
					{
						case Operation.New:
							await WriteOrder(item.Order1);
							break;
						case Operation.Updated:
							await DeleteOrder(item.Order1);
							await WriteOrder(item.Order2!);
							break;
						case Operation.Removed:
							await DeleteOrder(item.Order1);
							break;
						default:
							throw new NotSupportedException();
					}
				}
				catch (Exception ex)
				{
					logger.LogError(ex, "Transaction failed to write {Exception}, skipping", ex.Message);
				}
			}

			static int SortOnDataSource(Model.Activities.Activity activity, Model.Symbols.SymbolProfile x)
			{
				if (activity.Holding == null)
				{
					return 0;
				}

				var isCrypto = x.AssetSubClass == Model.Activities.AssetSubClass.CryptoCurrency && Datasource.GetUnderlyingDataSource(x.DataSource) == Datasource.COINGECKO;
				var isStock = x.AssetClass == Model.Activities.AssetClass.Equity && Datasource.GetUnderlyingDataSource(x.DataSource) == Datasource.YAHOO;

				if (isCrypto || isStock)
				{
					return 1;
				}

				return int.MaxValue;
			}
		}

		public async Task UpdateAccount(Model.Accounts.Account account)
		{
			var accounts = await GetAllAccounts();
			var existingAccount = accounts.Single(x => string.Equals(x.Name, account.Name, StringComparison.InvariantCultureIgnoreCase));
			var content = await restCall.DoRestGet($"api/v1/account/{existingAccount.Id}/balances");

			var balanceList = JsonConvert.DeserializeObject<BalanceList>(content!) ?? throw new NotSupportedException("Account not found");

			// Delete all balances that are not in the new list
			foreach (var item in balanceList.Balances?.Where(x => !account.Balance.Any(y => DateOnly.FromDateTime(x.Date) == y.Date)) ?? [])
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
				var existingBalance = balanceList.Balances?.SingleOrDefault(x => DateOnly.FromDateTime(x.Date) == newBalance.Date);
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

		public async Task SyncSymbolProfiles(IEnumerable<Model.Symbols.SymbolProfile> manualSymbolProfiles)
		{
			var existingProfiles = await GetAllSymbolProfiles();

			foreach (var manualSymbolProfile in manualSymbolProfiles)
			{
				var existingProfile = existingProfiles.SingleOrDefault(x => x.Symbol == manualSymbolProfile.Symbol && x.DataSource == manualSymbolProfile.DataSource);
				if (existingProfile == null)
				{
					var o1 = new JObject
					{
						["symbol"] = manualSymbolProfile.Symbol,
						["isin"] = manualSymbolProfile.ISIN,
						["name"] = manualSymbolProfile.Name,
						["comment"] = manualSymbolProfile.Comment,
						["assetClass"] = manualSymbolProfile.AssetClass.ToString(),
						["assetSubClass"] = manualSymbolProfile.AssetSubClass?.ToString(),
						["currency"] = manualSymbolProfile.Currency.Symbol,
						["datasource"] = manualSymbolProfile.DataSource.ToString(),
					};
					var res1 = o1.ToString();

					var r = await restCall.DoRestPost($"api/v1/admin/profile-data/{manualSymbolProfile.DataSource}/{manualSymbolProfile.Symbol}", res1);
					if (!r.IsSuccessStatusCode)
					{
						throw new NotSupportedException($"Creation failed {manualSymbolProfile.Symbol}");
					}

					logger.LogDebug("Created symbol profile {Symbol}", manualSymbolProfile.Symbol);
				}

				JObject mappingObject = [];

				JArray countries = [];
				foreach (var country in manualSymbolProfile.CountryWeight)
				{
					countries.Add(new JObject
					{
						["code"] = country.Code,
						["weight"] = country.Weight.ToString(),
						["continent"] = country.Continent,
						["name"] = country.Name,
					});
				}

				JArray sectors = [];
				foreach (var sector in manualSymbolProfile.SectorWeights)
				{
					sectors.Add(new JObject
					{
						["weight"] = sector.Weight.ToString(),
						["name"] = sector.Name,
					});
				}

				var o = new JObject
				{
					["name"] = manualSymbolProfile.Name,
					["assetClass"] = EnumMapper.ConvertAssetClassToString(manualSymbolProfile.AssetClass),
					["assetSubClass"] = EnumMapper.ConvertAssetSubClassToString(manualSymbolProfile.AssetSubClass),
					["comment"] = manualSymbolProfile.Comment ?? string.Empty,
					["symbolMapping"] = mappingObject,
					["countries"] = countries,
					["sectors"] = sectors
				};
				var res = o.ToString();

				try
				{
					var r = await restCall.DoRestPatch($"api/v1/admin/profile-data/{manualSymbolProfile.DataSource}/{manualSymbolProfile.Symbol}", res);
					if (!r.IsSuccessStatusCode)
					{
						throw new NotSupportedException($"Update failed on symbol {manualSymbolProfile.Symbol}");
					}
				}
				catch
				{
					throw new NotSupportedException($"Update failed on symbol {manualSymbolProfile.Symbol}.");
				}

				logger.LogDebug("Updated symbol profile {Symbol}", manualSymbolProfile.Symbol);
			}
		}

		public async Task SyncMarketData(Model.Symbols.SymbolProfile profile, ICollection<Model.Market.MarketData> list)
		{
			var content = await restCall.DoRestGet($"api/v1/market-data/{profile.DataSource}/{profile.Symbol}");
			var existingData = JsonConvert.DeserializeObject<MarketDataList>(content!)?.MarketData;

			foreach (var marketData in list)
			{
				var amount = (await currencyExchange.ConvertMoney(new Money(marketData.Currency, marketData.Close), profile.Currency, marketData.Date)).Amount;
				var value = existingData?.FirstOrDefault(x => x.Date == marketData.Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc))?.MarketPrice ?? 0;
				if (Math.Abs(value - amount) < 0.000001m)
				{
					continue;
				}

				var o = new JObject
				{
					["marketData"] = new JArray {
						new JObject
						{
							["date"] = marketData.Date.ToString("yyyy-MM-dd"),
							["marketPrice"] = amount
						}
					}
				};

				var res = o.ToString();

				var r = await restCall.DoRestPost($"api/v1/market-data/{profile.DataSource}/{profile.Symbol}", res);
				if (!r.IsSuccessStatusCode)
				{
					throw new NotSupportedException($"SetMarketPrice failed {profile.Symbol} {marketData.Date}");
				}

				logger.LogDebug("SetMarketPrice symbol {Symbol} {Date} @ {Amount}", profile.Symbol, marketData.Date, marketData.Close);
			}
		}

		private async Task<Account[]> GetAllAccounts()
		{
			var content = await restCall.DoRestGet($"api/v1/account") ?? throw new NotSupportedException();
			var rawAccounts = JsonConvert.DeserializeObject<AccountList>(content);
			var accounts = rawAccounts?.Accounts;

			if (accounts == null)
			{
				return [];
			}

			return accounts;
		}

		private async Task<List<Platform>> GetPlatforms()
		{
			var content = await restCall.DoRestGet($"api/v1/platform");

			if (content == null)
			{
				return [];
			}

			var rawPlatforms = JsonConvert.DeserializeObject<Platform[]>(content) ?? throw new NotSupportedException();
			return [.. rawPlatforms];
		}

		public async Task<List<Contract.SymbolProfile>> GetAllSymbolProfiles()
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
				content = await restCall.DoRestGet($"api/v1/market-data/{f.DataSource}/{f.Symbol}");
				var data = JsonConvert.DeserializeObject<MarketDataListNoMarketData>(content!);
				profiles.Add(data!.AssetProfile);
			}

			return profiles;
		}

		private async Task WriteOrder(Activity activity)
		{
			if (activity.Type == ActivityType.IGNORE)
			{
				logger.LogTrace("Skipping ignore transaction {Date} {Symbol} {Quantity} {Type}", activity.Date.ToInvariantString(), activity.SymbolProfile?.Symbol, activity.Quantity, activity.Type);

				return;
			}

			var url = $"api/v1/order";
			await restCall.DoRestPost(url, await ConvertToBody(activity));

			logger.LogInformation("Added transaction {Date} {Symbol} {Quantity} {Type}", activity.Date.ToInvariantString(), activity.SymbolProfile?.Symbol, activity.Quantity, activity.Type);
		}

		private async Task DeleteOrder(Activity activity)
		{
			if (string.IsNullOrWhiteSpace(activity.Id))
			{
				throw new NotSupportedException($"Deletion failed, no Id");
			}

			await restCall.DoRestDelete($"api/v1/order/{activity.Id}");
			logger.LogInformation("Deleted transaction {Date} {Symbol} {Quantity} {Type}", activity.Date.ToInvariantString(), activity.SymbolProfile?.Symbol, activity.Quantity, activity.Type);
		}

		private static Task<string> ConvertToBody(Activity activity)
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

	}
}
