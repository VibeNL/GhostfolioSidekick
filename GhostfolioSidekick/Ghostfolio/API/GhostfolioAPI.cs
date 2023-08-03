using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace GhostfolioSidekick.Ghostfolio.API
{
    public class GhostfolioAPI : IGhostfolioAPI
    {
        private readonly IMemoryCache memoryCache;
        private readonly MemoryCacheEntryOptions cacheEntryOptions;
        private ILogger<GhostfolioAPI> logger;

        string url = Environment.GetEnvironmentVariable("GHOSTFOLIO_URL");
        string accessToken = Environment.GetEnvironmentVariable("GHOSTFOLIO_ACCESTOKEN");

        public GhostfolioAPI(IMemoryCache memoryCache, ILogger<GhostfolioAPI> logger)
        {
            this.memoryCache = memoryCache;
            cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(TimeSpan.FromMinutes(5));
            this.logger = logger;

            if (url != null && url.EndsWith("/"))
            {
                url = url.Substring(0, url.Length - 1);
            }
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
            if (memoryCache.TryGetValue<decimal>(Tuple.Create(symbol, date.Date), out var result))
            {
                return result;
            }

            var options = new RestClientOptions(url)
            {
                ThrowOnAnyError = false,
                ThrowOnDeserializationError = false
            };

            var client = new RestClient(options);
            var request = new RestRequest($"{url}/api/v1/admin/market-data/YAHOO/{symbol}")
            {
                RequestFormat = DataFormat.Json
            };

            request.AddHeader("Authorization", $"Bearer {await GetAuthenticationToken()}");
            request.AddHeader("Content-Type", "application/json");

            var r = await client.ExecuteGetAsync(request);
            if (!r.IsSuccessStatusCode)
            {
                throw new NotSupportedException();
            }

            var market = JsonConvert.DeserializeObject<Market>(r.Content);

            foreach (var item in market.MarketData)
            {
                if (item.Date == date.Date)
                {
                    memoryCache.Set(Tuple.Create(symbol, date.Date), (decimal)item.MarketPrice, cacheEntryOptions);
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

            return new Order
            {
                AccountId = order.AccountId,
                Currency = order.Asset.Currency,  
                Asset = order.Asset,
                Comment = $"{order.Comment} | Original Price {order.UnitPrice}{order.Currency}, Fee {order.Fee}{order.Currency}",
                Date   = order.Date,
                Fee = (await GetExchangeRate(order.Currency, order.Asset.Currency, order.Date)) * order.Fee,
                Quantity = order.Quantity,
                Type = order.Type  ,
                UnitPrice = (await GetExchangeRate(order.Currency, order.Asset.Currency, order.Date)) * order.UnitPrice,
            };
        }

        private async Task<string> GetAuthenticationToken()
        {
            using (var client = new HttpClient())
            {
                string requestUri = $"{url}/api/v1/auth/anonymous/{accessToken}";
                var content = await client.GetStringAsync(requestUri);

                dynamic stuff = JsonConvert.DeserializeObject(content);
                var token = stuff.authToken.ToString();
                return token;
            }
        }

        private async Task WriteOrder(Order? order)
        {
            var options = new RestClientOptions(url)
            {
                ThrowOnAnyError = false,
                ThrowOnDeserializationError = false
            };

            var client = new RestClient(options);
            var request = new RestRequest($"{url}/api/v1/import")
            {
                RequestFormat = DataFormat.Json
            };

            request.AddHeader("Authorization", $"Bearer {await GetAuthenticationToken()}");
            request.AddHeader("Content-Type", "application/json");

            request.AddJsonBody(await ConvertToBody(order));
            var r = await client.ExecutePostAsync(request);
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

        public async Task<Asset> FindSymbolByISIN(string? isin)
        {
            if (memoryCache.TryGetValue<Asset>(isin, out var result))
            {
                return result;
            }

            var options = new RestClientOptions(url)
            {
                ThrowOnAnyError = false,
                ThrowOnDeserializationError = false
            };

            var client = new RestClient(options);
            var request = new RestRequest($"{url}/api/v1/symbol/lookup?query={isin}&includeIndices=true")
            {
                RequestFormat = DataFormat.Json
            };

            request.AddHeader("Authorization", $"Bearer {await GetAuthenticationToken()}");
            request.AddHeader("Content-Type", "application/json");

            var r = await client.ExecuteGetAsync(request);
            if (!r.IsSuccessStatusCode)
            {
                throw new NotSupportedException();
            }

            dynamic stuff = JsonConvert.DeserializeObject(r.Content);
            var asset = new Asset
            {
                Symbol = stuff.items[0].symbol,
                Currency = stuff.items[0].currency,
                DataSource = stuff.items[0].dataSource,
            };

            memoryCache.Set(isin, asset, cacheEntryOptions);
            return asset;
        }

        public async Task<decimal> GetExchangeRate(string sourceCurrency, string targetCurrency, DateTime date)
        {
            if (memoryCache.TryGetValue<decimal>(Tuple.Create(sourceCurrency, targetCurrency, date.Date), out var result))
            {
                return result;
            }

            var options = new RestClientOptions(url)
            {
                ThrowOnAnyError = false,
                ThrowOnDeserializationError = false
            };

            var client = new RestClient(options);
            var request = new RestRequest($"{url}/api/v1/exchange-rate/{sourceCurrency}-{targetCurrency}/{date:yyyy-MM-dd}")
            {
                RequestFormat = DataFormat.Json
            };

            request.AddHeader("Authorization", $"Bearer {await GetAuthenticationToken()}");
            request.AddHeader("Content-Type", "application/json");

            var r = await client.ExecuteGetAsync(request);
            if (!r.IsSuccessStatusCode)
            {
                throw new NotSupportedException();
            }

            dynamic stuff = JsonConvert.DeserializeObject(r.Content);
            var token = stuff.marketPrice.ToString();
            var rate = (decimal)decimal.Parse(token);

            memoryCache.Set(Tuple.Create(sourceCurrency, targetCurrency, date.Date), rate, cacheEntryOptions);
            return rate;
        }

        public async Task<Account> GetAccountByName(string name)
        {
            var options = new RestClientOptions(url)
            {
                ThrowOnAnyError = false,
                ThrowOnDeserializationError = false
            };

            var client = new RestClient(options);
            var request = new RestRequest($"{url}/api/v1/account")
            {
                RequestFormat = DataFormat.Json
            };

            request.AddHeader("Authorization", $"Bearer {await GetAuthenticationToken()}");
            request.AddHeader("Content-Type", "application/json");

            var r = await client.ExecuteGetAsync(request);
            if (!r.IsSuccessStatusCode)
            {
                throw new NotSupportedException();
            }

            var account = JsonConvert.DeserializeObject<AccountList>(r.Content);
            return account.Accounts.SingleOrDefault(x => string.Equals(x.Name, name, StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
