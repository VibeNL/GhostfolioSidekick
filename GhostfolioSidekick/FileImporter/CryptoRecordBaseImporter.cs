using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.FileImporter
{
	public abstract class CryptoRecordBaseImporter<T> : RecordBaseImporter<T>
	{
		protected CryptoRecordBaseImporter(IGhostfolioAPI api) : base(api)
		{
		}

		protected async Task<Money> GetCorrectUnitPrice(Money originalUnitPrice, Model.Asset? symbol, DateTime date)
		{
			if (originalUnitPrice.Amount > 0)
			{
				return originalUnitPrice;
			}

			// GetPrice from Ghostfolio
			var price = await api.GetMarketPrice(symbol, date);
			return price;
		}

		protected Model.Asset? ParseFindSymbolByISINResult(string assetName, string symbol, IEnumerable<Model.Asset> assets)
		{
			var cryptoOnly = assets.Where(x => x.AssetSubClass == "CRYPTOCURRENCY");
			var asset = cryptoOnly.FirstOrDefault(x => assetName == x.Name);
			if (asset != null)
			{
				return asset;
			}

			asset = cryptoOnly.FirstOrDefault(x => symbol == x.Symbol);
			if (asset != null)
			{
				return asset;
			}

			asset = cryptoOnly
				.OrderBy(x => x.Symbol.Length)
				.FirstOrDefault();

			return asset;
		}
	}
}
