namespace GhostfolioSidekick.Model.Market
{
	public record MarketData(
			Money Close,
			Money Open,
			Money High,
			Money Low,
			decimal TradingVolume,
			decimal NumberOfTransactions,
			Money VolumeWeightedAveragePrice,
			DateTime Date);
}