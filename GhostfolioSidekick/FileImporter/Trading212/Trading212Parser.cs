using CsvHelper;
using CsvHelper.Configuration;
using GhostfolioSidekick.Ghostfolio.API;
using System.Globalization;
using System.Reflection.PortableExecutable;

namespace GhostfolioSidekick.FileImporter.Trading212
{
	public class Trading212Parser : CSVSingleFileBaseImporter
	{
		public Trading212Parser(IGhostfolioAPI api) : base(api)
		{
		}

		protected override IEnumerable<HeaderMapping> ExpectedHeaders => new[]
		{
			// Datum,Tijd,Valutadatum,Product,ISIN,Omschrijving,FX,Mutatie,,Saldo,,Order Id
			new HeaderMapping{ DestinationHeader = DestinationHeader.OrderType, SourceName="Action" },
			new HeaderMapping{ DestinationHeader = DestinationHeader.Date, SourceName="Time" },
			new HeaderMapping{ DestinationHeader = DestinationHeader.Isin, SourceName="ISIN" },
			new HeaderMapping{ DestinationHeader = DestinationHeader.Symbol, SourceName="Ticker" },
			new HeaderMapping{ DestinationHeader = DestinationHeader.Undefined, SourceName="Name" },
			new HeaderMapping{ DestinationHeader = DestinationHeader.Quantity, SourceName="No. of shares" },
			new HeaderMapping{ DestinationHeader = DestinationHeader.UnitPrice, SourceName="Price / share" },
			new HeaderMapping{ DestinationHeader = DestinationHeader.Currency, SourceName="Currency (Price / share)" },
			new HeaderMapping{ DestinationHeader = DestinationHeader.Undefined, SourceName="Exchange rate" },
			new HeaderMapping{ DestinationHeader = DestinationHeader.Undefined, SourceName="Currency (Result)" },
			new HeaderMapping{ DestinationHeader = DestinationHeader.Undefined, SourceName="Total" },
			new HeaderMapping{ DestinationHeader = DestinationHeader.Undefined, SourceName="Currency (Total)" },
			new HeaderMapping{ DestinationHeader = DestinationHeader.Description, SourceName="Notes" },
			new HeaderMapping{ DestinationHeader = DestinationHeader.Reference, SourceName="ID" },
			new HeaderMapping{ DestinationHeader = DestinationHeader.Fee, SourceName="Currency conversion fee" },
			new HeaderMapping{ DestinationHeader = DestinationHeader.FeeCurrency, SourceName="Currency (Currency conversion fee)" },
		};

		protected override async Task<Asset> GetAsset(CsvReader csvReader)
		{
			if (GetOrderType(csvReader) == OrderType.IGNORE)
			{
				return null;
			}

			var isin = GetValue(csvReader, DestinationHeader.Isin);
			var symbol = await api.FindSymbolByISIN(isin);
			return symbol;
		}

		protected override string GetComment(CsvReader csvReader)
		{
			return $"Transaction Reference: [{GetValue(csvReader, DestinationHeader.Reference)}]";
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

		protected override CultureInfo GetCultureForParsingNumbers()
		{
			return new CultureInfo("en")
			{
				NumberFormat =
				{
					NumberDecimalSeparator = "."
				}
			};
		}

		protected override DateTime GetDate(CsvReader csvReader, DestinationHeader header)
		{
			var stringvalue = GetValue(csvReader, header);
			return DateTime.ParseExact(stringvalue, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture).Date;
		}

		protected override decimal GetFee(CsvReader csvReader)
		{
			return GetDecimalValue(csvReader, DestinationHeader.Fee);
		}

		protected override OrderType GetOrderType(CsvReader csvReader)
		{
			var order = csvReader.GetField(GetSourceFieldName(DestinationHeader.OrderType));
			switch (order)
			{
				case "Deposit":
					return OrderType.IGNORE;
				case "Market buy":
					return OrderType.BUY;
				default:
					throw new NotSupportedException();
			}
		}
	}
}
