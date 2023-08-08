using CsvHelper;
using GhostfolioSidekick.Ghostfolio.API;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.FileImporter.Trading212
{
	public class Trading212Parser : CSVSingleFileBaseImporter
	{
		public Trading212Parser(IGhostfolioAPI api) : base(api)
		{
		}

		protected override IEnumerable<HeaderMapping> ExpectedHeaders => throw new NotImplementedException();

		protected override Task<Asset> GetAsset(CsvReader csvReader)
		{
			throw new NotImplementedException();
		}

		protected override string GetComment(CsvReader csvReader)
		{
			throw new NotImplementedException();
		}

		protected override CultureInfo GetCultureForParsingNumbers()
		{
			throw new NotImplementedException();
		}

		protected override DateTime GetDate(CsvReader csvReader, DestinationHeader header)
		{
			throw new NotImplementedException();
		}

		protected override decimal GetFee(CsvReader csvReader)
		{
			throw new NotImplementedException();
		}

		protected override OrderType GetOrderType(CsvReader csvReader)
		{
			throw new NotImplementedException();
		}
	}
}
