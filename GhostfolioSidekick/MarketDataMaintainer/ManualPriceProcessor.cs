////using GhostfolioSidekick.Configuration;
////using GhostfolioSidekick.GhostfolioAPI;
////using GhostfolioSidekick.Model;
////using GhostfolioSidekick.Model.Activities.Types;
////using GhostfolioSidekick.Model.Market;
////using GhostfolioSidekick.Model.Symbols;
////using GhostfolioSidekick.Parsers;

////namespace GhostfolioSidekick.MarketDataMaintainer
////{
////	public class ManualPriceProcessor
////	{
////		private readonly IMarketDataService marketDataService;

////		public ManualPriceProcessor(IMarketDataService marketDataService)
////		{
////			this.marketDataService = marketDataService;
////		}

////		public async Task ProcessActivities(SymbolConfiguration symbolConfiguration, SymbolProfile symbolProfile, List<MarketData> marketDataList, List<BuySellActivity> unsortedActivities, List<HistoricData> historicData)
////		{
////			var sortedActivities = SortActivities(unsortedActivities);
////			for (var i = 0; i < sortedActivities.Count; i++)
////			{
////				var fromActivity = sortedActivities[i];
////				if (fromActivity?.UnitPrice == null)
////				{
////					continue;
////				}

////				BuySellActivity? toActivity = null;
////				if (i + 1 < sortedActivities.Count)
////				{
////					toActivity = sortedActivities[i + 1];
////				}

////				DateTime toDate = toActivity?.Date ?? DateTime.Today.AddDays(1);
////				for (var date = fromActivity.Date; date <= toDate; date = date.AddDays(1))
////				{
////					decimal expectedPrice = CalculateExpectedPrice(symbolConfiguration, fromActivity, toActivity, date, historicData);
////					var priceFromGhostfolio = marketDataList.SingleOrDefault(x => x.Date.Date == date.Date);

////					var diff = (priceFromGhostfolio?.MarketPrice.Amount ?? 0) - expectedPrice;
////					if (Math.Abs(diff) >= Constants.Epsilon)
////					{
////						var shouldSkip = ShouldSkipPriceUpdate(symbolConfiguration, priceFromGhostfolio, date);
////						if (shouldSkip)
////						{
////							continue;
////						}

////						await marketDataService.SetMarketPrice(symbolProfile, new Money(fromActivity.UnitPrice!.Currency, expectedPrice), date);
////					}
////				}
////			}
////		}

////		private List<BuySellActivity> SortActivities(List<BuySellActivity> activitiesForSymbol)
////		{
////			return activitiesForSymbol
////				.Where(x => x.UnitPrice?.Amount != 0)
////				.GroupBy(x => x.Date.Date)
////				.Select(x => x
////					.OrderBy(x => x.TransactionId)
////					.ThenByDescending(x => x.UnitPrice?.Amount ?? 0)
////					.ThenByDescending(x => x.Quantity)
////					.First())
////				.OrderBy(x => x.Date)
////				.ToList();
////		}

////		private decimal CalculateExpectedPrice(SymbolConfiguration symbolConfiguration, BuySellActivity fromActivity, BuySellActivity? toActivity, DateTime date, List<HistoricData> historicData)
////		{
////			var knownPrice = historicData.SingleOrDefault(x => x.Symbol == symbolConfiguration!.Symbol && x.Date.Date == date.Date);
////			if (knownPrice != null)
////			{
////				return knownPrice.Close;
////			}
////			else
////			{
////				var a = (decimal)(date - fromActivity.Date).TotalDays;
////				var b = (decimal)((toActivity?.Date ?? DateTime.Today.AddDays(1)) - date).TotalDays;

////				var percentage = a / (a + b);
////				decimal amountFrom = fromActivity.UnitPrice!.Amount;
////				decimal amountTo = toActivity?.UnitPrice?.Amount ?? fromActivity.UnitPrice?.Amount ?? 0;
////				return amountFrom + percentage * (amountTo - amountFrom);
////			}
////		}

////		private bool ShouldSkipPriceUpdate(SymbolConfiguration symbolConfiguration, MarketData? priceFromGhostfolio, DateTime date)
////		{
////			var scraperDefined = symbolConfiguration?.ManualSymbolConfiguration?.ScraperConfiguration != null;
////			var priceIsAvailable = (priceFromGhostfolio?.MarketPrice.Amount ?? 0) != 0;
////			var isToday = date >= DateTime.Today;
////			return scraperDefined && (priceIsAvailable || isToday);
////		}
////	}
////}