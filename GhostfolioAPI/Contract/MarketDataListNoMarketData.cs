using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.GhostfolioAPI.Contract
{
	[ExcludeFromCodeCoverage]
	public class MarketDataListNoMarketData
	{
		public required SymbolProfile AssetProfile { get; set; }
	}
}
