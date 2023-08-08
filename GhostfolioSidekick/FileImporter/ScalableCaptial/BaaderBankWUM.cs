using CsvHelper;
using GhostfolioSidekick.Ghostfolio.API;
using System.Globalization;

namespace GhostfolioSidekick.FileImporter.ScalableCaptial
{
	public class BaaderBankWUM : CSVSingleFileBaseImporter
	{
		public BaaderBankWUM(IGhostfolioAPI api) : base(api)
		{
		}

		protected override IEnumerable<HeaderMapping> ExpectedHeaders => new[]
		{
			new HeaderMapping{ DestinationHeader = DestinationHeader.Date, SourceName = "XXX-BUDAT" },
			new HeaderMapping{ DestinationHeader = DestinationHeader.UnitPrice, SourceName ="XXX-WPKURS" },
			new HeaderMapping{ DestinationHeader = DestinationHeader.Currency, SourceName ="XXX-WHGAB" },
			new HeaderMapping{ DestinationHeader = DestinationHeader.FeeCurrency, SourceName ="XXX-WHGAB" },
			new HeaderMapping{ DestinationHeader = DestinationHeader.Quantity, SourceName = "XXX-NW" },
			new HeaderMapping{ DestinationHeader = DestinationHeader.Isin, SourceName = "XXX-WPNR" },
			new HeaderMapping{ DestinationHeader = DestinationHeader.OrderType, SourceName = "XXX-WPGART" },
			new HeaderMapping{ DestinationHeader = DestinationHeader.Reference, SourceName = "XXX-EXTORDID" },
			new HeaderMapping{ DestinationHeader = DestinationHeader.Undefined, SourceName = "XXX-BELEGU" },
		};

		protected override decimal GetQuantity(CsvReader csvReader)
		{
			return Math.Abs(base.GetQuantity(csvReader));
		}

		protected override DateTime GetDate(CsvReader csvReader, DestinationHeader header)
		{
			var stringvalue = GetValue(csvReader, header);
			return DateTime.ParseExact(stringvalue, "yyyyMMdd", CultureInfo.InvariantCulture);
		}


		protected override decimal GetFee(CsvReader csvReader)
		{
			var isSavingsPlan = string.Equals(GetValue(csvReader, DestinationHeader.Undefined).Trim(), "00000000", StringComparison.InvariantCultureIgnoreCase);
			return isSavingsPlan ? 0 : -1;
		}

		protected override CultureInfo GetCultureForParsingNumbers()
		{
			return new CultureInfo("en")
			{
				NumberFormat =
				{
					NumberDecimalSeparator = ","
				}
			};
		}

		protected override Task<Asset> GetAsset(CsvReader csvReader)
		{
			var isin = GetValue(csvReader, DestinationHeader.Isin);
			var symbol = api.FindSymbolByISIN(isin);
			return symbol;
		}

		protected override string GetComment(CsvReader csvReader)
		{
			return $"Transaction Reference: [{GetValue(csvReader, DestinationHeader.Reference)}]";
		}

		protected override OrderType GetOrderType(CsvReader csvReader)
		{
			var order = csvReader.GetField(GetSourceFieldName(DestinationHeader.OrderType));
			switch (order)
			{
				case "Verkauf":
					return OrderType.SELL;
				case "Kauf":
					return OrderType.BUY;
				default:
					throw new NotSupportedException();
			}
		}
	}
}
