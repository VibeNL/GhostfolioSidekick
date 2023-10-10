using GhostfolioSidekick.Ghostfolio.API.Contract;
using GhostfolioSidekick.Ghostfolio.API.Mapper;
using GhostfolioSidekick.Model;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace GhostfolioSidekick.Ghostfolio.API
{
	public partial class GhostfolioAPI : IGhostfolioAPI
	{
		private readonly SymbolMapper mapper;
		private ILogger<GhostfolioAPI> logger;
		private readonly ActivityMapper activityMapper;

		string url = Environment.GetEnvironmentVariable("GHOSTFOLIO_URL");
		string accessToken = Environment.GetEnvironmentVariable("GHOSTFOLIO_ACCESTOKEN");
		private RestCall restCall;

		public GhostfolioAPI(
		IMemoryCache memoryCache, 
		ILogger<GhostfolioAPI> logger)
		{
			mapper = new SymbolMapper();
			this.logger = logger;
			this.activityMapper = activityMapper;
			if (url != null && url.EndsWith("/"))
			{
				url = url.Substring(0, url.Length - 1);
			}

			restCall = new RestCall(memoryCache, logger, url, accessToken);

			activityMapper = new ActivityMapper(new CurrentPriceCalculator(this));
		}

		public async Task<Model.Account?> GetAccountByName(string name)
		{
			var content = await restCall.DoRestGet($"api/v1/account", CacheDuration.Long());

			var rawAccounts = JsonConvert.DeserializeObject<AccountList>(content);
			var rawAccount = rawAccounts.Accounts.SingleOrDefault(x => string.Equals(x.Name, name, StringComparison.InvariantCultureIgnoreCase));

			if (rawAccount == null)
			{
				return null;
			}

			content = await restCall.DoRestGet($"api/v1/order?accounts={rawAccount.Id}", CacheDuration.None());
			var rawOrders = JsonConvert.DeserializeObject<RawActivityList>(content).Activities;

			var assets = new ConcurrentDictionary<string, Model.Asset>();

			return new Model.Account(
				rawAccount.Id,
				rawAccount.Name,
				new Balance(new Money(CurrencyHelper.ParseCurrency(rawAccount.Currency), rawAccount.Balance, DateTime.MinValue)),
				rawOrders.Select(x =>
				{
					var asset = assets.GetOrAdd(x.SymbolProfile.Symbol, (y) => ParseSymbolProfile(x.SymbolProfile));
					return new Model.Activity(
										ParseType(x.Type),
										asset,
										x.Date,
										x.Quantity,
										new Money(asset.Currency, x.UnitPrice, x.Date),
										new Money(asset.Currency, x.Fee, x.Date),
										x.Comment,
										ParseReference(x.Comment)
										);
				}).ToList()
				);
		}

		public async Task UpdateAccount(Model.Account account)
		{
			var existingAccount = await GetAccountByName(account.Name);
			// TODO update account!

			var balance = GetBalance(account.Balance);

			await UpdateBalance(account, balance);

			var newActivities = DateTimeCollisionFixer.Fix(account.Activities)
				.Select(x => activityMapper.ConvertToGhostfolioActivity(account, x))
				.Where(x => x != null)
				.Where(x => x.Type != Contract.ActivityType.IGNORE)
				.ToList();

			var content = await restCall.DoRestGet($"api/v1/order?accounts={existingAccount.Id}", CacheDuration.None());
			var existingActivities = JsonConvert.DeserializeObject<RawActivityList>(content).Activities;

			var mergeOrders = MergeOrders(newActivities, existingActivities).OrderBy(x => x.Operation).ToList();
			foreach (var mergeOrder in mergeOrders)
			{
				try
				{
					switch (mergeOrder.Operation)
					{
						case Operation.New:
							await WriteOrder(mergeOrder.Order1);
							break;
						case Operation.Duplicate:
							// Nothing to do!
							break;
						case Operation.Updated:
							await DeleteOrder(mergeOrder.Order2);
							await WriteOrder(mergeOrder.Order1);
							break;
						case Operation.Removed:
							await DeleteOrder(mergeOrder.Order2);
							break;
						default:
							throw new NotSupportedException();
					}
				}
				catch (Exception ex)
				{
					logger.LogError($"Transaction failed to write {ex}, skipping");
				}
			}
		}

		private decimal GetBalance(Balance balance)
		{
			return balance.Current(new CurrentPriceCalculator(this)).Amount;
		}

		public async Task<Money?> GetMarketPrice(Model.Asset asset, DateTime date)
		{
			var content = await restCall.DoRestGet($"api/v1/admin/market-data/{asset.DataSource}/{asset.Symbol}", CacheDuration.Short());
			var market = JsonConvert.DeserializeObject<Market>(content);

			var marketData = market.MarketData.FirstOrDefault(x => x.Date == date.Date);

			if (marketData == null)
			{
				return null;
			}

			return new Money(asset.Currency, marketData.MarketPrice, date);
		}

		public async Task<Model.Asset?> FindSymbolByISIN(string? identifier, Func<IEnumerable<Model.Asset>, Model.Asset?> selector)
		{
			identifier = mapper.MapIdentifier(identifier);

			var content = await restCall.DoRestGet($"api/v1/symbol/lookup?query={identifier.Trim()}", CacheDuration.Long());
			var rawAssetList = JsonConvert.DeserializeObject<AssetList>(content);

			var assets = rawAssetList.Items.Select(x => ParseSymbolProfile(x));

			if (selector == null)
			{
				return LogIfEmpty(assets.FirstOrDefault(), identifier);
			}

			return LogIfEmpty(selector(assets), identifier);
		}

		public async Task<Money?> GetConvertedPrice(Money money, Currency targetCurrency, DateTime date)
		{
			if (money == null || money.Currency == targetCurrency || (money.Amount) == 0)
			{
				return money;
			}

			var sourceCurrency = mapper.MapCurrency(money.Currency.Symbol);

			var content = await restCall.DoRestGet($"api/v1/exchange-rate/{sourceCurrency}-{targetCurrency.Symbol}/{date:yyyy-MM-dd}", CacheDuration.Short());

			dynamic stuff = JsonConvert.DeserializeObject(content);
			var token = stuff.marketPrice.ToString();
			var rate = (decimal)decimal.Parse(token);

			if (rate == 1)
			{
				return money;
			}

			return new Money(targetCurrency, rate * money.Amount, date);
		}

		private async Task WriteOrder(Contract.Activity activity)
		{
			if (activity.UnitPrice == 0 && activity.Quantity == 0)
			{
				logger.LogDebug($"Skipping empty transaction {activity.Date} {activity.Asset.Symbol} {activity.Quantity} {activity.Type}");
			}

			if (activity.Type == Contract.ActivityType.IGNORE)
			{
				logger.LogDebug($"Skipping ignore transaction {activity.Date} {activity.Asset.Symbol} {activity.Quantity} {activity.Type}");
			}

			url = $"api/v1/order";

			var r = await restCall.DoRestPost(url, await ConvertToBody(activity));
			bool emptyResponse = false;
			if (!r.IsSuccessStatusCode || (emptyResponse = r.Content.Equals("{\"activities\":[]}")))
			{
				var isduplicate = emptyResponse || (r.Content?.Contains("activities.1 is a duplicate activity") ?? false);
				if (isduplicate)
				{
					logger.LogDebug($"Duplicate transaction {activity.Date} {activity.Asset.Symbol} {activity.Quantity} {activity.Type}");
					return;
				}

				throw new NotSupportedException($"Insert Failed {activity.Date} {activity.Asset.Symbol} {activity.Quantity} {activity.Type}");
			}

			logger.LogInformation($"Added transaction {activity.Date} {activity.Asset?.Symbol} {activity.Quantity} {activity.Type}");
		}

		private async Task DeleteOrder(RawActivity? order)
		{
			var r = await restCall.DoRestDelete($"api/v1/order/{order.Id}");
			if (!r.IsSuccessStatusCode)
			{
				throw new NotSupportedException($"Deletion failed {order.Id}");
			}

			logger.LogInformation($"Deleted transaction {order.Id} {order.SymbolProfile.Symbol} {order.Date}");
		}

		private async Task UpdateBalance(Model.Account account, decimal balance)
		{
			var content = await restCall.DoRestGet($"api/v1/account", CacheDuration.Long());

			var rawAccounts = JsonConvert.DeserializeObject<AccountList>(content);
			var rawAccount = rawAccounts.Accounts.SingleOrDefault(x => string.Equals(x.Id, account.Id, StringComparison.InvariantCultureIgnoreCase));

			rawAccount.Balance = balance;

			var o = new JObject();
			o["balance"] = balance;
			o["comment"] = rawAccount.Comment;
			o["currency"] = rawAccount.Currency;
			o["id"] = rawAccount.Id;
			o["isExcluded"] = rawAccount.IsExcluded;
			o["name"] = rawAccount.Name;
			o["platformId"] = rawAccount.PlatformId;
			var res = o.ToString();

			await restCall.DoRestPut($"api/v1/account/{account.Id}", res);
		}

		private async Task<string> ConvertToBody(Contract.Activity activity)
		{
			var o = new JObject();
			o["accountId"] = activity.AccountId;
			o["comment"] = activity.Comment;
			o["currency"] = activity.Currency;
			o["dataSource"] = activity.Asset?.DataSource;
			o["date"] = activity.Date.ToString("o");
			o["fee"] = activity.Fee;
			o["quantity"] = activity.Quantity;

			if (activity.Type == Contract.ActivityType.INTEREST)
			{
				o["symbol"] = "Interest";
			}
			else
			{
				o["symbol"] = activity.Asset?.Symbol;
			}
			o["type"] = activity.Type.ToString();
			o["unitPrice"] = activity.UnitPrice;
			var res = o.ToString();
			return res;
		}

		private IEnumerable<MergeOrder> MergeOrders(IEnumerable<Contract.Activity> ordersFromFiles, IEnumerable<RawActivity> existingOrders)
		{
			var pattern = @"Transaction Reference: \[(.*?)\]";

			var existingOrdersWithMatchFlag = existingOrders.Select(x => new MatchRawActivity { Activity = x, IsMatched = false }).ToList();
			return ordersFromFiles.GroupJoin(existingOrdersWithMatchFlag,
				fo => fo.ReferenceCode,
				eo =>
				{
					if (string.IsNullOrWhiteSpace(eo.Activity.Comment))
					{
						return Guid.NewGuid().ToString();
					}

					var match = Regex.Match(eo.Activity.Comment, pattern);
					var key = (match.Groups.Count > 1 ? match?.Groups[1]?.Value : null) ?? string.Empty;
					return key;
				},
				(fo, eo) =>
				{
					if (fo != null && eo != null && eo.Any())
					{
						var other = eo.Single();
						other.IsMatched = true;

						if (AreEquals(fo, other.Activity))
						{
							return new MergeOrder(Operation.Duplicate, fo);
						}

						return new MergeOrder(Operation.Updated, fo, other.Activity);
					}
					else if (fo != null)
					{
						return new MergeOrder(Operation.New, fo);
					}
					else
					{
						throw new NotSupportedException();
					}
				}).Union(existingOrdersWithMatchFlag.Where(x => !x.IsMatched).Select(x => new MergeOrder(Operation.Removed, null, x.Activity)));
		}

		private bool AreEquals(Contract.Activity fo, RawActivity eo)
		{
			return
				(fo.Asset?.Symbol == eo.SymbolProfile?.Symbol || fo.Type == Contract.ActivityType.INTEREST) && // Interest create manual symbols
				fo.Quantity == eo.Quantity &&
				fo.UnitPrice == eo.UnitPrice &&
				fo.Fee == eo.Fee &&
				fo.Type == eo.Type &&
				fo.Date == eo.Date;
		}

		private Model.Asset? LogIfEmpty(Model.Asset? asset, string identifier)
		{
			if (asset == null)
			{
				logger.LogError($"Could not find {identifier} as a symbol");
			}

			return asset;
		}

		private static Model.Asset ParseSymbolProfile(Contract.SymbolProfile symbolProfile)
		{
			return new Model.Asset(
				CurrencyHelper.ParseCurrency(symbolProfile.Currency),
				symbolProfile.Symbol,
				symbolProfile.Name,
				symbolProfile.DataSource,
				symbolProfile.AssetSubClass,
				symbolProfile.AssetClass);
		}

		private static Model.Asset ParseSymbolProfile(Contract.Asset symbolProfile)
		{
			return new Model.Asset(
				CurrencyHelper.ParseCurrency(symbolProfile.Currency),
				symbolProfile.Symbol,
				symbolProfile.Name,
				symbolProfile.DataSource,
				symbolProfile.AssetSubClass,
				symbolProfile.AssetClass);
		}

		private static string ParseReference(string comment)
		{
			if (string.IsNullOrWhiteSpace(comment))
			{
				return null;
			}

			var pattern = @"Transaction Reference: \[(.*?)\]";
			var match = Regex.Match(comment, pattern);
			var key = (match.Groups.Count > 1 ? match?.Groups[1]?.Value : null) ?? string.Empty;
			return key;
		}

		private static Model.ActivityType ParseType(Contract.ActivityType type)
		{
			switch (type)
			{
				case Contract.ActivityType.BUY:
					return Model.ActivityType.Buy;
				case Contract.ActivityType.SELL:
					return Model.ActivityType.Sell;
				case Contract.ActivityType.DIVIDEND:
					return Model.ActivityType.Dividend;
				case Contract.ActivityType.INTEREST:
					return Model.ActivityType.Interest;
				default:
					throw new NotSupportedException($"ActivityType {type} not supported");
			}
		}

	}
}
