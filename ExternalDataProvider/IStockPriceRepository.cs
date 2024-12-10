using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.ExternalDataProvider
{
	public interface IStockPriceRepository
	{
		DateOnly MinDate { get; }

		string DataSource { get; }

		Task<IEnumerable<MarketData>> GetStockMarketData(SymbolProfile symbol, DateOnly fromDate);
	}
}
