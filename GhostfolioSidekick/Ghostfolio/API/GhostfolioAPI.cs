using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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

		public async Task UpdateOrders(IEnumerable<Order> orders)
		{
			var ordersByAccount = orders.GroupBy(x => x.AccountId);

			foreach (var group in ordersByAccount)
			{
				var mapped = group.Select(x => ConvertToNativeCurrency(x).Result).ToList();
				var existingOrders = await GetExistingOrders(group.First().AccountId);

				foreach (var mergeOrder in MergeOrders(mapped, existingOrders))
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
								break;
						}
					}
					catch (Exception ex)
					{
						logger.LogError($"Transaction failed to write {ex}, skipping");
					}
				}
			}
		}

		public async Task<decimal> GetMarketPrice(Asset asset, DateTime date)
		{
			var content = await restCall.DoRestGet($"api/v1/admin/market-data/{asset.DataSource}/{asset.Symbol}", CacheDuration.Short());
			var market = JsonConvert.DeserializeObject<Market>(content);
			return (decimal)(market.MarketData.FirstOrDefault(x => x.Date == date.Date)?.MarketPrice ?? 0);
		}

		public async Task<Asset> FindSymbolByISIN(string? identifier, Func<IEnumerable<Asset>, Asset> selector)
		{
			identifier = mapper.MapIdentifier(identifier);

			var content = await restCall.DoRestGet($"api/v1/symbol/lookup?query={identifier.Trim()}", CacheDuration.Long());
			var assetList = JsonConvert.DeserializeObject<AssetList>(content);

			if (selector == null)
			{
				return assetList.Items.FirstOrDefault();
			}

			return selector(assetList.Items);
		}

		public async Task<decimal> GetExchangeRate(string sourceCurrency, string targetCurrency, DateTime date)
		{
			if (sourceCurrency == targetCurrency)
			{
				return 1;
			}

			sourceCurrency = mapper.MapCurrency(sourceCurrency);

			var content = await restCall.DoRestGet($"api/v1/exchange-rate/{sourceCurrency}-{targetCurrency}/{date:yyyy-MM-dd}", CacheDuration.Short());

			dynamic stuff = JsonConvert.DeserializeObject(content);
			var token = stuff.marketPrice.ToString();
			var rate = (decimal)decimal.Parse(token);

			return rate;
		}

		public async Task<Account> GetAccountByName(string name)
		{
			var content = await restCall.DoRestGet($"api/v1/account", CacheDuration.Long());

			var account = JsonConvert.DeserializeObject<AccountList>(content);
			return account.Accounts.SingleOrDefault(x => string.Equals(x.Name, name, StringComparison.InvariantCultureIgnoreCase));
		}

		private async Task WriteOrder(Order order)
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

		private async Task DeleteOrder(RawOrder? order)
		{
			var r = await restCall.DoRestDelete($"api/v1/order/{order.Id}");
			if (!r.IsSuccessStatusCode)
			{
				throw new NotSupportedException($"Deletion failed {order.Id}");
			}

			logger.LogInformation($"Deleted transaction {order.Id} {order.SymbolProfile.Symbol} {order.Date}");
		}

		private async Task<Order> ConvertToNativeCurrency(Order order)
		{
			decimal Round(decimal value)
			{
				return Math.Round(value, 10);
			};

			decimal currencyConvertionRate = await GetExchangeRate(order.Currency, order.Asset.Currency, order.Date);
			decimal feeConvertionRate = order.Fee > 0 ? (await GetExchangeRate(order.FeeCurrency, order.Asset.Currency, order.Date)) : 1;

			var conversionComment = currencyConvertionRate != 1 ? $" | Original Price {order.UnitPrice}{order.Currency}, Fee {order.Fee}{order.Currency}" : string.Empty;
			return new Order
			{
				AccountId = order.AccountId,
				Currency = order.Asset.Currency,
				Asset = order.Asset,
				Comment = order.Comment + conversionComment,
				Date = order.Date,
				Fee = Round(feeConvertionRate * order.Fee),
				Quantity = Round(order.Quantity),
				Type = order.Type,
				UnitPrice = Round(currencyConvertionRate * order.UnitPrice),
				ReferenceCode = order.ReferenceCode
			};
		}

		private async Task<string> ConvertToBody(Order order)
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

		private IEnumerable<MergeOrder> MergeOrders(IEnumerable<Order> ordersFromFiles, IEnumerable<RawOrder> existingOrders)
		{
			var pattern = @"Transaction Reference: \[(.*?)\]";

			return ordersFromFiles.GroupJoin(existingOrders,
				fo => fo.ReferenceCode,
				eo =>
				{
					var match = Regex.Match(eo.Comment, pattern);
					var key = (match.Groups.Count > 1 ? match?.Groups[1]?.Value : null) ?? string.Empty;
					return key;
				},
				(fo, eo) =>
				{
					if (fo != null && eo != null && eo.Any())
					{
						if (AreEquals(fo, eo.Single()))
						{
							return new MergeOrder(Operation.Duplicate, fo);
						}

						return new MergeOrder(Operation.Updated, fo, eo.Single());
					}
					else if (fo != null)
					{
						return new MergeOrder(Operation.New, fo);
					}
					else
					{
						return new MergeOrder(Operation.Removed, null, eo.Single());
					}
				});
		}

		private bool AreEquals(Order fo, RawOrder eo)
		{
			return fo.Quantity == eo.Quantity &&
				fo.UnitPrice == eo.UnitPrice &&
				fo.Fee == eo.Fee &&
				fo.Type == eo.Type &&
				fo.Date == eo.Date;
		}

		private async Task<IEnumerable<RawOrder>> GetExistingOrders(string accountId)
		{
			var content = await restCall.DoRestGet($"api/v1/order?accounts={accountId}", CacheDuration.None());
			return JsonConvert.DeserializeObject<RawOrderList>(content).Activities;
		}

		private class MergeOrder
		{
			public MergeOrder(Operation operation, Order? order1)
			{
				Operation = operation;
				Order1 = order1;
			}

			public MergeOrder(Operation operation, Order? order1, RawOrder? order2) : this(operation, order1)
			{
				Order2 = order2;
			}

			public Operation Operation { get; }

			public Order Order1 { get; }

			public RawOrder Order2 { get; }
		}
	}
}
