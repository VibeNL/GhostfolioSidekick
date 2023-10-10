using GhostfolioSidekick.Model;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace GhostfolioSidekick.Ghostfolio.API
{
	public class GhostfolioAPI : IGhostfolioAPI
	{
		private readonly Mapper mapper;
		private ILogger<GhostfolioAPI> logger;

		string url = Environment.GetEnvironmentVariable("GHOSTFOLIO_URL");
		string accessToken = Environment.GetEnvironmentVariable("GHOSTFOLIO_ACCESTOKEN");
		private RestCall restCall;

		public GhostfolioAPI(IMemoryCache memoryCache, ILogger<GhostfolioAPI> logger)
		{
			mapper = new Mapper();
			this.logger = logger;

			if (url != null && url.EndsWith("/"))
			{
				url = url.Substring(0, url.Length - 1);
			}

			restCall = new RestCall(memoryCache, logger, url, accessToken);
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
				.Select(x => ConvertToGhostfolioActivity(account, x).Result)
				.Where(x => x != null)
				.Where(x => x.Type != ActivityType.IGNORE)
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

		private async Task WriteOrder(Activity activity)
		{
			if (activity.UnitPrice == 0 && activity.Quantity == 0)
			{
				logger.LogDebug($"Skipping empty transaction {activity.Date} {activity.Asset.Symbol} {activity.Quantity} {activity.Type}");
			}

			if (activity.Type == ActivityType.IGNORE)
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

		private async Task<Activity> ConvertToGhostfolioActivity(Model.Account account, Model.Activity activity)
		{
			decimal Round(decimal? value)
			{
				return Math.Round(value ?? 0, 10);
			};

			if (activity.ActivityType == Model.ActivityType.Interest)
			{
				return new Activity
				{
					AccountId = account.Id,
					Currency = account.Balance.Currency.Symbol,
					Asset = null,
					Comment = activity.Comment,
					Date = activity.Date,
					Fee = Round((await GetConvertedPrice(activity.Fee, account.Balance.Currency, activity.Date))?.Amount),
					Quantity = Round(activity.Quantity),
					Type = ParseType(activity.ActivityType),
					UnitPrice = Round((await GetConvertedPrice(activity.UnitPrice, account.Balance.Currency, activity.Date)).Amount),
					ReferenceCode = activity.ReferenceCode
				};
			}

			if (activity.Asset == null)
			{
				return null;
			}

			return new Activity
			{
				AccountId = account.Id,
				Currency = activity.Asset.Currency?.Symbol,
				Asset = new Asset
				{
					Symbol = activity.Asset.Symbol,
					AssetClass = activity.Asset.AssetClass,
					AssetSubClass = activity.Asset.AssetSubClass,
					Currency = activity.Asset.Currency.Symbol,
					DataSource = activity.Asset.DataSource,
					Name = activity.Asset.Name
				},
				Comment = activity.Comment,
				Date = activity.Date,
				Fee = Round((await GetConvertedPrice(activity.Fee, activity.Asset.Currency, activity.Date))?.Amount),
				Quantity = Round(activity.Quantity),
				Type = ParseType(activity.ActivityType),
				UnitPrice = Round((await GetConvertedPrice(activity.UnitPrice, activity.Asset.Currency, activity.Date)).Amount),
				ReferenceCode = activity.ReferenceCode
			};
		}

		private async Task<string> ConvertToBody(Activity activity)
		{
			var o = new JObject();
			o["accountId"] = activity.AccountId;
			o["comment"] = activity.Comment;
			o["currency"] = activity.Currency;
			o["dataSource"] = activity.Asset?.DataSource;
			o["date"] = activity.Date.ToString("o");
			o["fee"] = activity.Fee;
			o["quantity"] = activity.Quantity;

			if (activity.Type == ActivityType.INTEREST)
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

		private IEnumerable<MergeOrder> MergeOrders(IEnumerable<Activity> ordersFromFiles, IEnumerable<RawActivity> existingOrders)
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

		private bool AreEquals(Activity fo, RawActivity eo)
		{
			return
				(fo.Asset?.Symbol == eo.SymbolProfile?.Symbol || fo.Type == ActivityType.INTEREST) && // Interest create manual symbols
				fo.Quantity == eo.Quantity &&
				fo.UnitPrice == eo.UnitPrice &&
				fo.Fee == eo.Fee &&
				fo.Type == eo.Type &&
				fo.Date == eo.Date;
		}

		private sealed class MergeOrder
		{
			public MergeOrder(Operation operation, Activity order1)
			{
				Operation = operation;
				Order1 = order1;
				Order2 = null;
			}

			public MergeOrder(Operation operation, Activity order1, RawActivity? order2) : this(operation, order1)
			{
				Order2 = order2;
			}

			public Operation Operation { get; }

			public Activity Order1 { get; }

			public RawActivity? Order2 { get; }
		}

		private Model.Asset? LogIfEmpty(Model.Asset? asset, string identifier)
		{
			if (asset == null)
			{
				logger.LogError($"Could not find {identifier} as a symbol");
			}

			return asset;
		}

		private static Model.Asset ParseSymbolProfile(SymbolProfile symbolProfile)
		{
			return new Model.Asset(
				CurrencyHelper.ParseCurrency(symbolProfile.Currency),
				symbolProfile.Symbol,
				symbolProfile.Name,
				symbolProfile.DataSource,
				symbolProfile.AssetSubClass,
				symbolProfile.AssetClass);
		}

		private static Model.Asset ParseSymbolProfile(Asset symbolProfile)
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

		private static Model.ActivityType ParseType(ActivityType type)
		{
			switch (type)
			{
				case ActivityType.BUY:
					return Model.ActivityType.Buy;
				case ActivityType.SELL:
					return Model.ActivityType.Sell;
				case ActivityType.DIVIDEND:
					return Model.ActivityType.Dividend;
				case ActivityType.INTEREST:
					return Model.ActivityType.Interest;
				default:
					throw new NotSupportedException($"ActivityType {type} not supported");
			}
		}

		private ActivityType ParseType(Model.ActivityType? type)
		{
			switch (type)
			{
				case null:
					return ActivityType.IGNORE;
				case Model.ActivityType.Buy:
					return ActivityType.BUY;
				case Model.ActivityType.Sell:
					return ActivityType.SELL;
				case Model.ActivityType.Dividend:
					return ActivityType.DIVIDEND;
				case Model.ActivityType.Send:
					return ActivityType.SELL; // TODO: 
				case Model.ActivityType.Receive:
					return ActivityType.BUY; // TODO: 
				case Model.ActivityType.Convert:
					return ActivityType.IGNORE; // TODO: 
				case Model.ActivityType.Interest:
					return ActivityType.INTEREST;
				case Model.ActivityType.Gift:
					return ActivityType.BUY; // TODO: 
				case Model.ActivityType.LearningReward:
					return ActivityType.IGNORE; // TODO: 
				case Model.ActivityType.StakingReward:
					return ActivityType.IGNORE; // TODO: 
				default:
					throw new NotSupportedException($"ActivityType {type} not supported");
			}

		}

		private class MatchRawActivity
		{
			public RawActivity Activity { get; set; }
			public bool IsMatched { get; set; }
		}

		private class CurrentPriceCalculator : ICurrentPriceCalculator
		{
			private GhostfolioAPI ghostfolioAPI;

			public CurrentPriceCalculator(GhostfolioAPI ghostfolioAPI)
			{
				this.ghostfolioAPI = ghostfolioAPI;
			}

			public Money GetConvertedPrice(Money item, Currency targetCurrency, DateTime timeOfRecord)
			{
				return ghostfolioAPI.GetConvertedPrice(item, targetCurrency, timeOfRecord).Result;
			}
		}
	}
}
