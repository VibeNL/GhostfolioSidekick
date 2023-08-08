using CsvHelper;
using GhostfolioSidekick.Ghostfolio.API;
using System.Globalization;

namespace GhostfolioSidekick.FileImporter.ScalableCaptial
{
	public class BaaderBankRKK : CSVSingleFileBaseImporter
	{
		private static Asset EmptyAsset = new Asset();

		public BaaderBankRKK(IGhostfolioAPI api) : base(api)
		{
		}

		protected override IEnumerable<HeaderMapping> ExpectedHeaders => new[]
		{
			new HeaderMapping{ DestinationHeader = DestinationHeader.OrderType, SourceName = "XXX-UMART" },
			new HeaderMapping{ DestinationHeader = DestinationHeader.Symbol, SourceName = "XXX-TEXT1" }, // Symbol name
			new HeaderMapping{ DestinationHeader = DestinationHeader.Isin, SourceName = "XXX-TEXT2" }, // ISIN
			new HeaderMapping{ DestinationHeader = DestinationHeader.UnitPrice, SourceName = "XXX-SALDO" },
			new HeaderMapping{ DestinationHeader = DestinationHeader.Currency, SourceName = "XXX-WHG" },
			new HeaderMapping{ DestinationHeader = DestinationHeader.FeeCurrency, SourceName = "XXX-WHG" },
			new HeaderMapping{ DestinationHeader = DestinationHeader.Reference, SourceName = "XXX-REFNR1" },
			new HeaderMapping{ DestinationHeader = DestinationHeader.Date, SourceName = "XXX-VALUTA" },
			new HeaderMapping{ DestinationHeader = DestinationHeader.Quantity, SourceName = "XXX-TEXT3" },
		};

		protected override decimal GetUnitPrice(CsvReader csvReader)
		{
			var totalprice = base.GetUnitPrice(csvReader);
			return totalprice / GetQuantity(csvReader);
		}

		protected override decimal GetQuantity(CsvReader csvReader)
		{
			if (GetOrderType(csvReader) == OrderType.IGNORE || GetOrderType(csvReader) == OrderType.FEE)
			{
				return -1;
			}

			var quantity = GetValue(csvReader, DestinationHeader.Quantity).Replace("STK ", string.Empty);
			return decimal.Parse(quantity, GetCultureForParsingNumbers());
		}

		protected override DateTime GetDate(CsvReader csvReader, DestinationHeader header)
		{
			var stringvalue = GetValue(csvReader, header);
			return DateTime.ParseExact(stringvalue, "yyyyMMdd", CultureInfo.InvariantCulture);
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

		protected override decimal GetFee(CsvReader csvReader)
		{
			return 0;
		}

		protected override async Task<Asset> GetAsset(CsvReader csvReader)
		{
			if (GetOrderType(csvReader) == OrderType.FEE)
			{
				return EmptyAsset;
			}

			if (GetOrderType(csvReader) == OrderType.IGNORE)
			{
				return null;
			}

			var isin = GetValue(csvReader, DestinationHeader.Isin).Replace("ISIN ", string.Empty);
			var symbol = api.FindSymbolByISIN(isin);
			return await symbol;
		}

		protected override string GetComment(CsvReader csvReader)
		{
			return $"Transaction Reference: [{GetValue(csvReader, DestinationHeader.Reference)}]";
		}

		protected override OrderType GetOrderType(CsvReader csvReader)
		{
			var order = csvReader.GetField(GetSourceFieldName(DestinationHeader.OrderType));

			if (order == "Coupons/Dividende")
			{
				return OrderType.DIVIDEND;
			}

			if (order?.StartsWith("Ordergeb") ?? false)
			{
				return OrderType.FEE;
			}

			return OrderType.IGNORE;
		}
	}
}
