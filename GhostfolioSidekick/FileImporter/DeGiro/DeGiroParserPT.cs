using CsvHelper.Configuration;
using GhostfolioSidekick.Ghostfolio.API;
using System.Globalization;

namespace GhostfolioSidekick.FileImporter.DeGiro
{
	public class DeGiroParserPT : DeGiroParserBase<DeGiroRecordPT>
	{
		public DeGiroParserPT(IGhostfolioAPI api) : base(api)
		{
		}

		protected override CsvConfiguration GetConfig()
		{
			return new CsvConfiguration(new CultureInfo("pt-PT"))
			{
				HasHeaderRecord = true,
				CacheFields = true,
				Delimiter = ",",
			};
		}
	}
}