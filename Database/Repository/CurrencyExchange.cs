using GhostfolioSidekick.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.Database.Repository
{
	public class CurrencyExchange(IDbContextFactory<DatabaseContext> databaseContextFactory, ILogger<CurrencyExchange> logger) : ICurrencyExchange, IDisposable
	{
		private readonly Dictionary<string, Dictionary<DateOnly, decimal>> _preloadedExchangeRates = new();
		private bool _isPreloaded = false;
		private readonly SemaphoreSlim _preloadSemaphore = new(1, 1);
		private bool _disposed = false;

		public async Task<Money> ConvertMoney(Money money, Currency currency, DateOnly date)
		{
			var sourceCurrency = money.Currency;
			var targetCurrency = currency;
			var exchangeRate = await GetExchangeRate(date, sourceCurrency, targetCurrency);

			return new Money(currency, money.Times(exchangeRate).Amount);
		}

		public async Task PreloadAllExchangeRates()
		{
			if (_isPreloaded)
			{
				return;
			}

			await _preloadSemaphore.WaitAsync();
			try
			{
				if (_isPreloaded)
				{
					return;
				}

				logger.LogInformation("Starting preload of all exchange rates...");
				await PreloadAllExchangeRatesInternal();
				_isPreloaded = true;
				logger.LogInformation("Exchange rates preload completed. Loaded {Count} currency pairs.", _preloadedExchangeRates.Count);
			}
			finally
			{
				_preloadSemaphore.Release();
			}
		}

		public void ClearPreloadedCache()
		{
			_preloadSemaphore.Wait();
			try
			{
				_preloadedExchangeRates.Clear();
				_isPreloaded = false;
				logger.LogInformation("Exchange rates cache cleared.");
			}
			finally
			{
				_preloadSemaphore.Release();
			}
		}

		private async Task<decimal> GetExchangeRate(DateOnly searchDate, Currency sourceCurrency, Currency targetCurrency)
		{
			// If the currencies are the same, return 1
			if (sourceCurrency == targetCurrency)
			{
				return 1;
			}

			// If the exchange rate is known, return it
			var exchangeRate = sourceCurrency.GetKnownExchangeRate(targetCurrency);
			if (exchangeRate != 0)
			{
				return exchangeRate;
			}

			var searchSourceCurrency = sourceCurrency.GetSourceCurrency();
			var searchTargetCurrency = targetCurrency.GetSourceCurrency();

			// If the currencies are the same, return 1
			if (searchSourceCurrency == searchTargetCurrency)
			{
				return searchSourceCurrency.Item2 * (1m / searchTargetCurrency.Item2);
			}

			var symbolKey = $"{searchSourceCurrency.Item1.Symbol}{searchTargetCurrency.Item1.Symbol}";

			// Try to get from preloaded cache first
			if (_isPreloaded && _preloadedExchangeRates.TryGetValue(symbolKey, out var cachedRates))
			{
				if (cachedRates.TryGetValue(searchDate, out exchangeRate) && exchangeRate != 0)
				{
					return exchangeRate * searchSourceCurrency.Item2 * (1m / searchTargetCurrency.Item2);
				}

				// Use the last known value from cache. Maybe a holiday or weekend?
				var values = cachedRates
					.Where(x => x.Key < searchDate)
					.OrderByDescending(x => x.Key);
				var count = values.Count();
				var lastKnownRate = values
					.FirstOrDefault();

				if (lastKnownRate.Value != 0)
				{
					return lastKnownRate.Value * searchSourceCurrency.Item2 * (1m / searchTargetCurrency.Item2);
				}

				logger.LogWarning("No exchange rate found for {FromCurrency} to {ToCurrency} in preloaded cache. Trying to query database.", sourceCurrency, targetCurrency);
			}

			// Fall back to database query if not preloaded or not found in cache
			using var databaseContext = await databaseContextFactory.CreateDbContextAsync();
			exchangeRate = await databaseContext.SymbolProfiles
								.Where(x => x.Symbol == symbolKey)
								.SelectMany(x => x.MarketData)
								.Where(x => x.Date == searchDate)
								.Select(x => x.Close.Amount)
								.FirstOrDefaultAsync();

			if (exchangeRate == 0)
			{
				// Use the last known value. Maybe a holiday or weekend?
				exchangeRate = await databaseContext.SymbolProfiles
									.Where(x => x.Symbol == symbolKey)
									.SelectMany(x => x.MarketData)
									.Where(x => x.Date < searchDate)
									.OrderByDescending(x => x.Date)
									.Select(x => x.Close.Amount)
									.FirstOrDefaultAsync();

				if (exchangeRate == 0)
				{
					logger.LogWarning("No exchange rate found for {FromCurrency} to {ToCurrency}. Using 1:1 rate.", sourceCurrency, targetCurrency);
					return 1;
				}
			}

			return exchangeRate * searchSourceCurrency.Item2 * (1m / searchTargetCurrency.Item2);
		}

		private async Task PreloadAllExchangeRatesInternal()
		{
			using var databaseContext = await databaseContextFactory.CreateDbContextAsync();

			try
			{
				// Get all currency exchange rate symbols (currency pairs) with their market data in a single query
				// Use EF.Property to access shadow properties within the LINQ query
				var allExchangeRateData = await databaseContext.MarketDatas
					.Where(md => EF.Property<string>(md, "SymbolProfileSymbol").Length == 6) // Currency pairs are typically 6 characters (USDEUR, GBPUSD, etc.)
					.Select(md => new
					{
						Symbol = EF.Property<string>(md, "SymbolProfileSymbol"),
						md.Date,
						md.Close.Amount
					})
					.ToListAsync();

				// Group and cache the data
				_preloadedExchangeRates.Clear();
				foreach (var group in allExchangeRateData.Where(x => x.Amount != 0).GroupBy(x => x.Symbol))
				{
					_preloadedExchangeRates[group.Key] = group
						.ToDictionary(x => x.Date, x => x.Amount);
				}
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Failed to preload exchange rates from MarketDatas. This may be expected in unit tests.");
				// Clear any partial data that might have been loaded
				_preloadedExchangeRates.Clear();
			}
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed)
			{
				if (disposing)
				{
					_preloadSemaphore?.Dispose();
				}
				_disposed = true;
			}
		}
	}
}
