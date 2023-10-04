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
				new Money(CurrencyHelper.ParseCurrency(rawAccount.Currency), rawAccount.Balance),
				rawOrders.Select(x =>
				{
					var asset = assets.GetOrAdd(x.SymbolProfile.Symbol, (y) => ParseSymbolProfile(x.SymbolProfile));
					return new Model.Activity(
										ParseType(x.Type),
										asset,
										x.Date,
										x.Quantity,
										new Money(asset.Currency, x.UnitPrice),
										new Money(asset.Currency, x.Fee),
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

			var newActivities = DateTimeCollisionFixer.Fix(account.Activities).Select(x => ConvertToGhostfolioActivity(account, x).Result).ToList();

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

		public async Task<Money?> GetMarketPrice(Model.Asset asset, DateTime date)
		{
			var content = await restCall.DoRestGet($"api/v1/admin/market-data/{asset.DataSource}/{asset.Symbol}", CacheDuration.Short());
			var market = JsonConvert.DeserializeObject<Market>(content);

			var marketData = market.MarketData.FirstOrDefault(x => x.Date == date.Date);

			if (marketData == null)
			{
				return null;
			}

			return new Money(asset.Currency, marketData.MarketPrice);
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
			if (money.Currency == targetCurrency || (money.Amount ?? 0) == 0)
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

			return new Money(targetCurrency, rate * money.Amount);
		}

		private async Task WriteOrder(Activity order)
		{
			if (order.UnitPrice == 0 && order.Quantity == 0)
			{
				logger.LogDebug($"Skipping empty transaction {order.Date} {order.Asset.Symbol} {order.Quantity} {order.Type}");
			}

			var r = await restCall.DoRestPost($"api/v1/import", await ConvertToBody(order));
			bool emptyResponse = false;
			if (!r.IsSuccessStatusCode || (emptyResponse = r.Content.Equals("{\"activities\":[]}")))
			{
				var isduplicate = emptyResponse || (r.Content?.Contains("activities.1 is a duplicate activity") ?? false);
				if (isduplicate)
				{
					logger.LogDebug($"Duplicate transaction {order.Date} {order.Asset.Symbol} {order.Quantity} {order.Type}");
					return;
				}

				throw new NotSupportedException($"Insert Failed {order.Date} {order.Asset.Symbol} {order.Quantity} {order.Type}");
			}

			logger.LogInformation($"Added transaction {order.Date} {order.Asset.Symbol} {order.Quantity} {order.Type}");
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

		private async Task<Activity> ConvertToGhostfolioActivity(Model.Account account, Model.Activity order)
		{
			decimal Round(decimal? value)
			{
				return Math.Round(value ?? 0, 10);
			};

			return new Activity
			{
				AccountId = account.Id,
				Currency = order.Asset?.Currency?.Symbol,
				Asset = new Asset { Symbol = order.Asset?.Symbol },
				Comment = order.Comment,
				Date = order.Date,
				Fee = Round((await GetConvertedPrice(order.Fee, order.Asset?.Currency, order.Date)).Amount),
				Quantity = Round(order.Quantity),
				Type = ParseType(order.Type),
				UnitPrice = Round((await GetConvertedPrice(order.UnitPrice, order.Asset?.Currency, order.Date)).Amount),
				ReferenceCode = order.ReferenceCode
			};
		}

		private async Task<string> ConvertToBody(Activity order)
		{
			var o = new JObject();
			var r = new JObject
			{
				["activities"] = new JArray()
				{
					o
				}
			};

			o["accountId"] = order.AccountId;
			o["comment"] = order.Comment;
			o["currency"] = order.Currency;
			o["dataSource"] = order.Asset.DataSource;
			o["date"] = order.Date.ToString("o");
			o["fee"] = order.Fee;
			o["quantity"] = order.Quantity;
			o["symbol"] = order.Asset.Symbol;
			o["type"] = order.Type.ToString();
			o["unitPrice"] = order.UnitPrice;
			var res = r.ToString();
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
				fo.Asset?.Symbol == eo.SymbolProfile?.Symbol &&
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
					return ActivityType.IGNORE; // TODO: 
				case Model.ActivityType.Gift:
					return ActivityType.IGNORE; // TODO: 
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
	}
}
