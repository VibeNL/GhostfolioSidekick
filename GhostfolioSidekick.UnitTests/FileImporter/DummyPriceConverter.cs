using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.UnitTests.FileImporter
{
	internal class DummyPriceConverter : ICurrentPriceCalculator
	{
		public Money GetConvertedPrice(Money item, Currency targetCurrency, DateTime timeOfRecord)
		{
			return new Money(targetCurrency, item.Amount, timeOfRecord);
		}

		public static DummyPriceConverter Instance { get { return new DummyPriceConverter(); } }
	}
}
