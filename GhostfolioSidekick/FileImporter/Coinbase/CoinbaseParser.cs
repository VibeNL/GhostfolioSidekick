using CsvHelper.Configuration;
using GhostfolioSidekick.Ghostfolio.API;
using System.Globalization;

namespace GhostfolioSidekick.FileImporter.Coinbase
{
	public class CoinbaseParser : RecordBaseImporter<CoinbaseRecord>
	{
		private IGhostfolioAPI api;

		public CoinbaseParser(IGhostfolioAPI api) : base(api)
		{
			this.api = api;
		}

		protected override Task<Order?> ConvertOrder(CoinbaseRecord record, Account account, IEnumerable<CoinbaseRecord> allRecords)
		{
			throw new NotImplementedException();
		}

		protected override CsvConfiguration GetConfig()
		{
			return new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				HasHeaderRecord = true,
				CacheFields = true,
				Delimiter = ",",
			};
		}

		protected override StreamReader GetStreamReader(string file)
		{
			var sr = base.GetStreamReader(file);

			for (var i = 0; i < 7; i++)
			{
				sr.ReadLine();
			}

			return sr;
		}
	}
}
