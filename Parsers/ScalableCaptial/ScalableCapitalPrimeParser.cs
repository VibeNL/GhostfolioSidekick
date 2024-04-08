using CsvHelper.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.ScalableCaptial
{
	public class ScalableCapitalPrimeParser : RecordBaseImporter<ScalableCapitalPrimeRecord>
	{
		private readonly ICurrencyMapper currencyMapper;

		public ScalableCapitalPrimeParser(ICurrencyMapper currencyMapper)
		{
			this.currencyMapper = currencyMapper;
		}

		protected override IEnumerable<PartialActivity> ParseRow(ScalableCapitalPrimeRecord record, int rowNumber)
		{
			throw new NotSupportedException("This method should not be called.");
		}

		protected override CsvConfiguration GetConfig()
		{
			return new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				HasHeaderRecord = true,
				CacheFields = true,
				Delimiter = ";",
			};
		}
	}
}
