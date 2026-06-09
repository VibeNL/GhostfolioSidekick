using CoinGecko.Net.Interfaces;
using CoinGecko.Net.Objects.Models;
using CryptoExchange.Net.Objects;
using GhostfolioSidekick.Cryptocurrency;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.ExternalDataProvider.CoinGecko
{
	public class CoinGeckoRepository(
			ILogger<CoinGeckoRepository> logger,
			ICoinGeckoRestClient coinGeckoRestClient
			) :
		 ISymbolMatcher,
		 IStockPriceRepository
	{
		public string DataSource => Datasource.COINGECKO;

		public DateOnly MinDate => DateOnly.FromDateTime(DateTime.Today.AddDays(-365));

		public bool AllowedForDeterminingHolding => true;

		public async Task<SymbolProfile?> MatchSymbol(PartialSymbolIdentifier[] symbolIdentifiers)
		{
			PartialSymbolIdentifier? id = symbolIdentifiers.FirstOrDefault(x => x.AllowedAssetSubClasses != null && x.AllowedAssetSubClasses.Contains(AssetSubClass.CryptoCurrency));
			if (id == null)
			{
				return null;
			}

			CoinGeckoAsset? coinGeckoAsset = await GetCoinGeckoAsset(id.Identifier);
			if (coinGeckoAsset == null)
			{
				return null;
			}

			return new SymbolProfile(
				coinGeckoAsset.Symbol,
				coinGeckoAsset.Name,
				[
					new SymbolIdentifier { Identifier = coinGeckoAsset.Symbol, IdentifierType = IdentifierType.Ticker },
					new SymbolIdentifier { Identifier = coinGeckoAsset.Name, IdentifierType = IdentifierType.Name }
				],
				Currency.USD with { },
				Datasource.COINGECKO,
				AssetClass.Liquidity,
				AssetSubClass.CryptoCurrency,
				[],
				[])
			{
				WebsiteUrl = $"https://www.coingecko.com/en/coins/{coinGeckoAsset.Id}"
			};
		}

		public async Task<IEnumerable<MarketData>> GetStockMarketData(SymbolProfile symbol, DateOnly fromDate)
		{
			CoinGeckoAsset? coinGeckoAsset = await GetCoinGeckoAsset(symbol.Symbol);
			if (coinGeckoAsset == null)
			{
				return [];
			}

			WebCallResult<CoinGeckoOhlc[]> longRange = await RetryPolicyHelper
							.GetFallbackPolicy<WebCallResult<CoinGeckoOhlc[]>>(logger)
							.WrapAsync(RetryPolicyHelper
								.GetRetryPolicy(logger))
								.ExecuteAsync(async () =>
								{
									WebCallResult<CoinGeckoOhlc[]>? response = await coinGeckoRestClient.Api.GetOhlcAsync(coinGeckoAsset.Id, "usd", 365);

									return response == null || !response.Success ? throw new InvalidOperationException(response?.Error?.Message) : response;
								});

			WebCallResult<CoinGeckoOhlc[]> shortRange = await RetryPolicyHelper
							.GetFallbackPolicy<WebCallResult<CoinGeckoOhlc[]>>(logger)
							.WrapAsync(RetryPolicyHelper
								.GetRetryPolicy(logger))
								.ExecuteAsync(async () =>
								{
									WebCallResult<CoinGeckoOhlc[]>? response = await coinGeckoRestClient.Api.GetOhlcAsync(coinGeckoAsset.Id, "usd", 30);

									return response == null || !response.Success ? throw new InvalidOperationException(response?.Error?.Message) : response;
								});

			List<MarketData> list = [];
			foreach (CoinGeckoOhlc candle in ((longRange?.Data) ?? []).Union(shortRange?.Data ?? []))
			{
				MarketData item = new(
								Currency.USD,
								candle.Close,
								candle.Open,
								candle.High,
								candle.Low,
								0,
								DateOnly.FromDateTime(candle.Timestamp.Date));
				list.Add(item);
			}

			// Add the existing market data
			list = [.. list.Union(symbol.MarketData)];

			return list.OrderByDescending(x => x.Date).DistinctBy(x => x.Date);
		}

		private async Task<CoinGeckoAsset?> GetCoinGeckoAsset(string identifier)
		{
			if (string.IsNullOrWhiteSpace(identifier))
			{
				return null;
			}

			WebCallResult<CoinGeckoAsset[]> coinGeckoAssets = await coinGeckoRestClient.Api.GetAssetsAsync();
			if (coinGeckoAssets == null || !coinGeckoAssets.Success)
			{
				return null;
			}

			List<CoinGeckoAsset> list = coinGeckoAssets.Data
				.Select(asset => new CoinGeckoAsset
				{
					Id = asset.Id,
					Name = asset.Name,
					Symbol = asset.Symbol
				})
				.ToList();

			return GetAsset(identifier, list);

			static CoinGeckoAsset? GetAsset(string identifier, List<CoinGeckoAsset> assets)
			{
				return assets
					.Where(x => x.Symbol.Equals(identifier, StringComparison.InvariantCultureIgnoreCase))
					.OrderByDescending(IsKnown)
					.ThenBy(x => x.Name.Length)
					.ThenBy(x => x.Id)
					.FirstOrDefault();
			}
		}

		private static bool IsKnown(CoinGeckoAsset x)
		{
			string fullName = CryptoMapper.Instance.GetFullname(x.Symbol) ?? string.Empty;
			return x.Name.Equals(fullName, StringComparison.InvariantCultureIgnoreCase);
		}
	}
}
