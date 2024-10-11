using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Symbols;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.MarketDataMaintainer
{
	internal class CurrencyGathererTask(ICurrencyRepository currencyRepository, IActivityRepository activityRepository, IMarketDataRepository marketDataRepository) : IScheduledWork
	{
		public TaskPriority Priority => TaskPriority.CurrencyGatherer;

		public TimeSpan ExecutionFrequency => TimeSpan.FromHours(1);

		public async Task DoWork()
		{
			var activities = await activityRepository.GetAllActivities();
			
			var currencies = new Dictionary<Currency, DateTime>();
			foreach (var activity in activities.OrderBy(x => x.Date))
			{
				switch (activity)
				{
					case ActivityWithQuantityAndUnitPrice activityWithQuantityAndUnitPrice:
						{
							Currency? key = activityWithQuantityAndUnitPrice.UnitPrice?.Currency;
							if (key != null && !currencies.ContainsKey(key))
							{
								currencies.Add(key, activity.Date);
							}
						}
						break;
					default:
						break;
				}
			}

			var currenciesMatches = currencies.SelectMany((l) => currencies, (l, r) => Tuple.Create(l, r)).Where(x => x.Item1.Key != x.Item2.Key);

			foreach (var match in currenciesMatches)
			{
				if (match.Item1.Key.IsKnownPair(match.Item2.Key))
				{
					continue;
				}

				string symbolString = match.Item1.Key.Symbol + match.Item2.Key.Symbol;
				DateOnly fromDate = DateOnly.FromDateTime(new DateTime[] { match.Item1.Value, match.Item2.Value }.Min());

				var symbol = await marketDataRepository.GetSymbolProfileBySymbol(symbolString);
				
				// Check if we need to update our data
				if (symbol != null && symbol.MarketData.Count > 0 &&
						DateOnly.FromDateTime(symbol.MarketData.Min(x => x.Date)) == fromDate &&
						symbol.MarketData.Max(x => x.Date) == DateTime.Today)
				{
					continue;
				}

				var currencyHistory = await currencyRepository.GetCurrencyHistory(match.Item1.Key, match.Item2.Key, fromDate);
				if (currencyHistory != null)
				{
					symbol = await marketDataRepository.GetSymbolProfileBySymbol(symbolString);
					if (symbol == null)
					{
						symbol = new SymbolProfile(symbolString, symbolString, [], match.Item1.Key with {}, Datasource.YAHOO, AssetClass.Undefined, null, [], []);
					}

					symbol.MarketData.Clear();

					foreach (var item in currencyHistory)
					{
						symbol.MarketData.Add(item);
					}

					await marketDataRepository.Store(symbol).ConfigureAwait(false);
				}
			}
		}
	}
}
