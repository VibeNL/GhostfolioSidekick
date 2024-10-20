//using GhostfolioSidekick.Model;
//using GhostfolioSidekick.Model.Symbols;
//using Microsoft.Extensions.Logging;

//namespace GhostfolioSidekick.GhostfolioAPI.Strategies
//{
//	public class ApiStrategyBase
//	{
//		private readonly IMarketDataService marketDataService;
//		private readonly ILogger logger;

//		public ApiStrategyBase(IMarketDataService marketDataService, ILogger logger)
//		{
//			this.marketDataService = marketDataService;
//			this.logger = logger;
//		}

//		protected async Task<Money> GetUnitPrice(SymbolProfile symbolProfile, DateTime date)
//		{
//			var marketDataProfile = await marketDataService.GetMarketData(symbolProfile.Symbol, symbolProfile.DataSource);
//			var marketDate = marketDataProfile?.MarketData?.SingleOrDefault(x => x.Date.Date == date.Date);

//			if (marketDate == null)
//			{
//				marketDate = marketDataProfile?.MarketData?.SingleOrDefault(x => x.Date.Date == date.Date.AddDays(1) || x.Date.Date == date.Date.AddDays(-1));

//				if (marketDate == null)
//				{
//					logger.LogDebug($"No market data found for {symbolProfile.Symbol} on {date.Date}. Assuming price of 0 until Ghostfolio has determined the price");
//				}
//				else
//				{
//					logger.LogDebug($"No market data found for {symbolProfile.Symbol} on {date.Date}. Assuming price of the {marketDate.Date} until Ghostfolio has determined the price");
//				}
//			}

//			return new Money(symbolProfile!.Currency, marketDate?.MarketPrice.Amount ?? 0);
//		}
//	}
//}