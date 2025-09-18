using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services
{
	public class CurrencyConvertion(IDbContextFactory<DatabaseContext> databaseContextFactory, ICurrencyExchange currencyExchange) : ICurrencyConvertion
	{
		public async Task ConvertAll(Currency targetCurrency)
		{
			// Preload all exchange rates first to avoid repeated cache lookups
			await currencyExchange.PreloadAllExchangeRates();

			// Process in batches to control memory usage in WASM
			await ConvertSnapshotsInBatches(targetCurrency);
			await ConvertBalancesInBatches(targetCurrency);
		}

		private async Task ConvertSnapshotsInBatches(Currency targetCurrency)
		{
			const int batchSize = 50; // Smaller batches for WASM
			var skip = 0;
			
			while (true)
			{
				using (var databaseContext = await databaseContextFactory.CreateDbContextAsync())
				{
					// Load batch with tracking enabled for updates
					var batch = await databaseContext
						.CalculatedSnapshots
						.Where(x => x.TotalValue.Currency != targetCurrency ||
								   x.CurrentUnitPrice.Currency != targetCurrency ||
								   x.TotalInvested.Currency != targetCurrency)
						.Skip(skip)
						.Take(batchSize)
						.ToListAsync();

					if (batch.Count == 0)
					{
						break;
					}

					// Process each item sequentially (WASM is single-threaded)
					foreach (var snapShot in batch)
					{
						// Convert all money fields for this snapshot
						if (snapShot.TotalValue.Currency != targetCurrency)
						{
							snapShot.TotalValue = await currencyExchange.ConvertMoney(
								snapShot.TotalValue, targetCurrency, snapShot.Date);
						}

						if (snapShot.CurrentUnitPrice.Currency != targetCurrency)
						{
							snapShot.CurrentUnitPrice = await currencyExchange.ConvertMoney(
								snapShot.CurrentUnitPrice, targetCurrency, snapShot.Date);
						}

						if (snapShot.TotalInvested.Currency != targetCurrency)
						{
							snapShot.TotalInvested = await currencyExchange.ConvertMoney(
								snapShot.TotalInvested, targetCurrency, snapShot.Date);
						}
					}

					// Save this batch before processing the next one to avoid memory buildup
					await databaseContext.SaveChangesAsync();
					skip += batchSize;
				}
			}
		}

		private async Task ConvertBalancesInBatches(Currency targetCurrency)
		{
			const int batchSize = 50; // Smaller batches for WASM
			var skip = 0;
			
			while (true)
			{
				using (var databaseContext = await databaseContextFactory.CreateDbContextAsync())
				{
					// Load batch with tracking enabled for updates
					var batch = await databaseContext
						.Accounts
						.SelectMany(x => x.Balance)
						.Where(x => x.Money.Currency != targetCurrency)
						.Skip(skip)
						.Take(batchSize)
						.ToListAsync();

					if (batch.Count == 0)
					{
						break;
					}

					// Process each item sequentially (WASM is single-threaded)
					foreach (var balance in batch)
					{
						balance.Money = await currencyExchange.ConvertMoney(
							balance.Money,
							targetCurrency,
							balance.Date);
					}

					// Save this batch before processing the next one to avoid memory buildup
					await databaseContext.SaveChangesAsync();
					skip += batchSize;
				}
			}
		}
	}
}
