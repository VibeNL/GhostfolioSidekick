using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

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

		public async Task Write(IEnumerable<Order> orders)
		{
			foreach (var order in orders.OrderBy(x => x.Date))
			{
				try
				{
					await WriteOrder(order);
				}
				catch (Exception ex)
				{
					logger.LogError($"Transaction failed to write {ex}, skipping");
				}
			}
		}

		public async Task<decimal> GetMarketPrice(string symbol, DateTime date)
		{
			var content = await restCall.DoRestGet($"api/v1/admin/market-data/YAHOO/{symbol}");
			var market = JsonConvert.DeserializeObject<Market>(content);

			foreach (var item in market.MarketData)
			{
				if (item.Date == date.Date)
				{
					return (decimal)item.MarketPrice;
				}
			}

			return 0;
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

			double Round(decimal value)
			{
				return decimal.ToDouble(Math.Round(value, 10));
			};

			order = await ConvertOrderToNativeCurrencyIfNeeded(order);

			o["accountId"] = order.AccountId;
			o["comment"] = order.Comment;
			o["currency"] = order.Currency;
			o["dataSource"] = order.Asset.DataSource;
			o["date"] = order.Date.ToString("o");
			o["fee"] = Round(order.Fee);
			o["quantity"] = Round(order.Quantity);
			o["symbol"] = order.Asset.Symbol;
			o["type"] = order.Type.ToString();
			o["unitPrice"] = Round(order.UnitPrice);
			var res = r.ToString();
			return res;
		}

		private async Task<Order> ConvertOrderToNativeCurrencyIfNeeded(Order order)
		{
			if (order.Currency == order.Asset.Currency)
			{
				return order;
			}

			decimal currencyConvertionRate = (await GetExchangeRate(order.Currency, order.Asset.Currency, order.Date));
			decimal feeConvertionRate = order.Fee > 0 ? (await GetExchangeRate(order.FeeCurrency, order.Asset.Currency, order.Date)) : 1;
			return new Order
			{
				AccountId = order.AccountId,
				Currency = order.Asset.Currency,
				Asset = order.Asset,
				Comment = $"{order.Comment} | Original Price {order.UnitPrice}{order.Currency}, Fee {order.Fee}{order.Currency}",
				Date = order.Date,
				Fee = feeConvertionRate * order.Fee,
				Quantity = order.Quantity,
				Type = order.Type,
				UnitPrice = currencyConvertionRate * order.UnitPrice,
			};
		}

		private async Task WriteOrder(Order? order)
		{
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

				throw new NotSupportedException();
			}

			logger.LogInformation($"Added transaction {order.Date} {order.Asset.Symbol} {order.Quantity} {order.Type}");
		}

		public async Task<Asset> FindSymbolByISIN(string? identifier)
		{
			identifier = mapper.MapIdentifier(identifier);

			var content = await restCall.DoRestGet($"api/v1/symbol/lookup?query={identifier.Trim()}&includeIndices=true");
			dynamic stuff = JsonConvert.DeserializeObject(content);
			var asset = new Asset
			{
				Symbol = stuff.items[0].symbol,
				Currency = stuff.items[0].currency,
				DataSource = stuff.items[0].dataSource,
			};

			return asset;
		}

		public async Task<decimal> GetExchangeRate(string sourceCurrency, string targetCurrency, DateTime date)
		{
			sourceCurrency = mapper.MapCurrency(sourceCurrency);

			var content = await restCall.DoRestGet($"api/v1/exchange-rate/{sourceCurrency}-{targetCurrency}/{date:yyyy-MM-dd}");

			dynamic stuff = JsonConvert.DeserializeObject(content);
			var token = stuff.marketPrice.ToString();
			var rate = (decimal)decimal.Parse(token);

			return rate;
		}

		public async Task<Account> GetAccountByName(string name)
		{
			var content = await restCall.DoRestGet($"api/v1/account");

			var account = JsonConvert.DeserializeObject<AccountList>(content);
			return account.Accounts.SingleOrDefault(x => string.Equals(x.Name, name, StringComparison.InvariantCultureIgnoreCase));
		}
	}
}
