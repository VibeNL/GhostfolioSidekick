//using GhostfolioSidekick.Configuration;
//using GhostfolioSidekick.Model;
//using GhostfolioSidekick.Model.Activities;
//using GhostfolioSidekick.Model.Compare;
//using Microsoft.Extensions.Logging;

//namespace GhostfolioSidekick.GhostfolioAPI.Strategies
//{
//	public class ApplyDustCorrection : ApiStrategyBase, IHoldingStrategy
//	{
//		private readonly Settings settings;
//		private readonly IExchangeRateService exchangeRateService;
//		private readonly ILogger<ApplyDustCorrection> logger;

//		public ApplyDustCorrection(
//			Settings settings,
//			IMarketDataService marketDataService,
//			IExchangeRateService exchangeRateService,
//			ILogger<ApplyDustCorrection> logger) : base(marketDataService, logger)
//		{
//			this.settings = settings;
//			this.exchangeRateService = exchangeRateService;
//			this.logger = logger;
//		}

//		public int Priority => (int)StrategiesPriority.ApplyDustCorrection;

//		public async Task Execute(Holding holding)
//		{
//			if (holding.SymbolProfile == null)
//			{
//				return;
//			}

//			var allActivities = holding.Activities.OfType<IActivityWithQuantityAndUnitPrice>().OrderBy(x => x.Date).ToList();

//			var totalDustValueQuantity = allActivities
//					.Sum(x => x!.Quantity);

//			var threshold = settings.DustThreshold;
//			if (holding.SymbolProfile?.AssetSubClass == AssetSubClass.CryptoCurrency)
//			{
//				threshold = settings.CryptoWorkaroundDustThreshold;
//			}

//			var unitPrice = await GetUnitPrice(holding.SymbolProfile!, DateTime.Today);
//			if (unitPrice == null || unitPrice.Amount == 0)
//			{
//				unitPrice = allActivities.Last(x => x.UnitPrice != null)?.UnitPrice;
//			}

//			var targetCurrency = new Currency(settings.DustCurrency);
//			if (unitPrice == null)
//			{
//				unitPrice = new Money(targetCurrency, 1);
//			}

//			unitPrice = new Money(
//				new Currency(settings.DustCurrency),
//				unitPrice.Amount * await exchangeRateService.GetConversionRate(unitPrice.Currency, targetCurrency, DateTime.Today));

//			var totalDustValue = unitPrice.Times(totalDustValueQuantity);

//			if (totalDustValue.Amount == 0 || Math.Abs(totalDustValue.Amount) >= threshold)
//			{
//				// No dust to correct
//				return;
//			}

//			var accounts = allActivities.Select(x => x.Account).Distinct();
//			foreach (var account in accounts)
//			{
//				var activities = allActivities
//					.Where(x => x.Account == account)
//					.OfType<IActivityWithQuantityAndUnitPrice>()
//					.ToList();

//				var amount = activities
//					.Sum(x => x!.Quantity);

//				// Should always be a sell or send as we have dust!
//				var lastActivity = activities
//					.LastOrDefault(x => x!.Quantity < 0);
//				if (lastActivity == null)
//				{
//					return;
//				}

//				decimal dustValue = unitPrice.Times(amount).Amount;

//				if (dustValue == 0)
//				{
//					continue;
//				}

//				// Remove activities after the last sell activity
//				RemoveActivitiesAfter(holding, activities, lastActivity);

//				// Get the new amount
//				amount = activities.Sum(x => x!.Quantity);

//				// Update unit price of the last activity if possible
//				if (lastActivity.UnitPrice != null)
//				{
//					lastActivity.UnitPrice = new Money(
//										lastActivity.UnitPrice.Currency,
//										lastActivity.UnitPrice.Amount * (lastActivity.Quantity / (lastActivity.Quantity - amount)));
//					if (lastActivity.UnitPrice.Amount < 0)
//					{
//						lastActivity.UnitPrice.Amount = 0;
//					}
//				}

//				// Update the quantity of the last activity
//				lastActivity.Quantity -= amount;

//				logger.LogDebug(
//					"Dust corrected for symbol {Symbol}, account {Account}. Dust amount was {Dustvalue}{Currency}. Quantity {Quantity}",
//					holding.SymbolProfile,
//					account,
//					dustValue,
//					unitPrice.Currency,
//					amount);
//			}

//			return;
//		}

//		private static void RemoveActivitiesAfter(Holding holding, List<IActivityWithQuantityAndUnitPrice> activities, IActivityWithQuantityAndUnitPrice lastActivity)
//		{
//			int index = activities.IndexOf(lastActivity) + 1;
//			activities.RemoveRange(index, activities.Count - index);
//			holding.Activities.RemoveAll(x => x.Account == lastActivity.Account && !activities.Contains(x));
//		}
//	}
//}
