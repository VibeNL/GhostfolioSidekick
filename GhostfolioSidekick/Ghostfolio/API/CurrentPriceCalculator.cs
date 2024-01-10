using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.Ghostfolio.API
{
	public class CurrentPriceCalculator : ICurrentPriceCalculator
	{
		private GhostfolioAPI ghostfolioAPI;

		public CurrentPriceCalculator(GhostfolioAPI ghostfolioAPI)
		{
			this.ghostfolioAPI = ghostfolioAPI;
		}

		public Money? GetConvertedPrice(Money? item, Currency targetCurrency, DateTime timeOfRecord)
		{
			return ghostfolioAPI.GetConvertedPrice(item, targetCurrency, timeOfRecord).Result;
		}
	}
}
