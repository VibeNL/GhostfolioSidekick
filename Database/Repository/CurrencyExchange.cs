using GhostfolioSidekick.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.Database.Repository
{
	public class CurrencyExchange(
			IDbContextFactory<DatabaseContext> databaseContextFactory,
			MemoryCache memoryCache,
			ILogger<CurrencyExchange> logger) : ICurrencyExchange
	{
		private readonly SemaphoreSlim _preloadSemaphore = new(1, 1);

		public Task ClearCache()
		{
			// Remove all entries from the memory cache
			var keys = memoryCache.Keys
				.Where(kvp => kvp is ExchangeRateKey)
				.ToList();

			foreach (var key in keys)
			{
				memoryCache.Remove(key);
			}

			return Task.CompletedTask;
		}

		public async Task<Money> ConvertMoney(Money money, Currency currency, DateOnly date)
		{
			var sourceCurrency = money.Currency;
			var targetCurrency = currency;
			var exchangeRate = await GetExchangeRate(date, sourceCurrency, targetCurrency);

			return new Money(currency, money.Times(exchangeRate).Amount);
		}

		private async Task<decimal> GetExchangeRate(DateOnly searchDate, Currency sourceCurrency, Currency targetCurrency)
		{
			if (sourceCurrency == Currency.NONE || targetCurrency == Currency.NONE)
			{
				throw new ArgumentException("Source or target currency cannot be NONE.");
			}

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

			// Try to get from preloaded cache first
			if (!memoryCache.TryGetValue(new ExchangeRateKey(searchSourceCurrency.Item1, searchTargetCurrency.Item1),
				out IDictionary<DateOnly, decimal>? cachedRates))
			{
				await PreloadAllExchangeRates();
				if (!memoryCache.TryGetValue(new ExchangeRateKey(searchSourceCurrency.Item1, searchTargetCurrency.Item1), out cachedRates))
				{
					logger.LogWarning("No exchange rates found for {FromCurrency} to {ToCurrency}.", sourceCurrency, targetCurrency);
					return 1m; // Default to 1 if no rate is found
				}
			}

			if (cachedRates == null)
			{
				logger.LogWarning("No exchange rates found for {FromCurrency} to {ToCurrency}.", sourceCurrency, targetCurrency);
				return 1m; // Default to 1 if no rate is found
			}

			if (cachedRates.TryGetValue(searchDate, out exchangeRate) && exchangeRate != 0)
			{
				return exchangeRate * searchSourceCurrency.Item2 * (1m / searchTargetCurrency.Item2);
			}

			// Use the last known value from cache. Maybe a holiday or weekend?
			var values = cachedRates
				.Where(x => x.Key < searchDate)
				.OrderByDescending(x => x.Key);
			var lastKnownRate = values.FirstOrDefault();

			if (lastKnownRate.Value != 0)
			{
				return lastKnownRate.Value * searchSourceCurrency.Item2 * (1m / searchTargetCurrency.Item2);
			}

			logger.LogWarning("No exchange rate found for {FromCurrency} to {ToCurrency}.", sourceCurrency, targetCurrency);
			return 1m; // Default to 1 if no rate is found
		}

		public async Task PreloadAllExchangeRates()
		{
			await _preloadSemaphore.WaitAsync();
			try
			{
				logger.LogInformation("Starting preload of all exchange rates...");
				await PreloadAllExchangeRatesInternal();
				logger.LogInformation("Preloaded of all exchange rates...");
			}
			finally
			{
				_preloadSemaphore.Release();
			}
		}

		private async Task PreloadAllExchangeRatesInternal()
		{
			using var databaseContext = await databaseContextFactory.CreateDbContextAsync();

			try
			{
				var allExchangeRateData = await databaseContext.CurrencyExchangeRates
				.SelectMany(profile => profile.Rates, (profile, rate) => new
				{
					profile.SourceCurrency,
					profile.TargetCurrency,
					rate.Date,
					rate.Close.Amount
				})
				.ToListAsync();

				foreach (var group in allExchangeRateData.Where(x => x.Amount != 0).GroupBy(x => new { x.SourceCurrency, x.TargetCurrency }))
				{
					memoryCache.Set(
						new ExchangeRateKey(group.Key.SourceCurrency, group.Key.TargetCurrency),
						group.ToDictionary(x => x.Date, x => x.Amount));
				}
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Failed to preload exchange rates.");
			}
		}

		private sealed record ExchangeRateKey(Currency SourceCurrency, Currency TargetCurrency);
	}
}
