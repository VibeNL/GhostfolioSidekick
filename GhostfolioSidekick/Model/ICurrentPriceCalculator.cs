namespace GhostfolioSidekick.Model
{
	public interface ICurrentPriceCalculator
	{
		public Money? GetConvertedPrice(Money? item, Currency targetCurrency, DateTime timeOfRecord);
	}
}
