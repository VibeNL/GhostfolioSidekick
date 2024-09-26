namespace GhostfolioSidekick.Model.Market
{
	public record MarketData(
			Money Close,
			Money Open,
			Money High,
			Money Low,
			decimal TradingVolume,
			DateTime Date);
}