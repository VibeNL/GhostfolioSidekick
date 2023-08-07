using GhostfolioSidekick.Ghostfolio.API;

namespace GhostfolioSidekick.FileImporter.ScalableCaptial
{

	public class ScalableCapitalParser : IFileImporter
	{
		private IEnumerable<IFileImporter> fileImporters;

		private IGhostfolioAPI api;

		public ScalableCapitalParser(IGhostfolioAPI api)
		{
			this.api = api;
			fileImporters = new IFileImporter[] {
				new BaaderBankRKK(api),
				new BaaderBankWUM(api),
			};
		}

		public async Task<bool> CanConvertOrders(string file)
		{
			return fileImporters.Any(x => x.CanConvertOrders(file).Result);
		}

		public async Task<IEnumerable<Order>> ConvertToOrders(string accountName, string filename)
		{
			var orders = await fileImporters.Single(x => x.CanConvertOrders(filename).Result).ConvertToOrders(accountName, filename);

			// TODO: Postprocess

			return orders;
		}
	}
}
