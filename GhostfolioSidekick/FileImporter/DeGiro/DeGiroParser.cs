using CsvHelper;
using CsvHelper.Configuration;
using GhostfolioSidekick.Ghostfolio.API;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GhostfolioSidekick.FileImporter.DeGiro
{
	public class DeGiroParser : CSVSingleFileBaseImporter
	{
		public DeGiroParser(IGhostfolioAPI api) : base(api)
		{
		}

		protected override IEnumerable<HeaderMapping> ExpectedHeaders => new[]
		{
			// Datum,Tijd,Valutadatum,Product,ISIN,Omschrijving,FX,Mutatie,,Saldo,,Order Id
			new HeaderMapping{ DestinationHeader = DestinationHeader.Date, SourceName="Datum" },
			new HeaderMapping{ DestinationHeader = DestinationHeader.Undefined, SourceName="Tijd" },
			new HeaderMapping{ DestinationHeader = DestinationHeader.Undefined, SourceName="Valutadatum" },
			new HeaderMapping{ DestinationHeader = DestinationHeader.Symbol, SourceName="Product" },
			new HeaderMapping{ DestinationHeader = DestinationHeader.Isin, SourceName="ISIN" },
			new HeaderMapping{ DestinationHeader = DestinationHeader.Description, SourceName="Omschrijving" },
			new HeaderMapping{ DestinationHeader = DestinationHeader.Undefined, SourceName="FX" },
			new HeaderMapping{ DestinationHeader = DestinationHeader.Currency, SourceName="Mutatie" },
			new HeaderMapping{ DestinationHeader = DestinationHeader.FeeCurrency, SourceName="Mutatie" },
			new HeaderMapping{ DestinationHeader = DestinationHeader.Undefined, SourceName="Saldo" },
			new HeaderMapping{ DestinationHeader = DestinationHeader.Reference, SourceName="Order Id" },
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

		protected override DateTime GetDate(CsvReader csvReader, DestinationHeader header)
		{
			var stringvalue = GetValue(csvReader, header);
			return DateTime.ParseExact(stringvalue, "dd-MM-yyyy", CultureInfo.InvariantCulture);
		}

		protected override decimal GetQuantity(CsvReader csvReader)
		{
			if (GetOrderType(csvReader) == OrderType.FEE)
			{
				return -1;
			}

			var quantity = Regex.Match(GetValue(csvReader, DestinationHeader.Description), $"Koop (?<amount>\\d+) @ (?<price>.*) EUR").Groups[1].Value;
			return decimal.Parse(quantity, GetCultureForParsingNumbers());
		}

		protected override decimal GetUnitPrice(CsvReader csvReader)
		{
			if (GetOrderType(csvReader) == OrderType.FEE)
			{
				return Math.Abs(decimal.Parse(csvReader.GetField(8), GetCultureForParsingNumbers()));
			}

			var quantity = Regex.Match(GetValue(csvReader, DestinationHeader.Description), $"Koop (?<amount>\\d+) @ (?<price>.*) EUR").Groups[2].Value;
			return decimal.Parse(quantity, GetCultureForParsingNumbers());
		}

		protected override string GetComment(CsvReader csvReader)
		{
			return $"Transaction Reference: [{GetValue(csvReader, DestinationHeader.Reference)}]";
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
			return -1;
		}

		protected override OrderType GetOrderType(CsvReader csvReader)
		{
			var omschrijving = GetValue(csvReader, DestinationHeader.Description);

			if (omschrijving.Contains("Koop"))
			{
				return OrderType.BUY;
			}

			if (omschrijving.Contains("Transactiekosten"))
			{
				return OrderType.FEE;
			}

			// TODO, implement other options
			return OrderType.IGNORE;
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

		protected override IEnumerable<Order> PostProcessList(List<Order> list)
		{
			// Match Fee with Transaction
			var group = list.GroupBy(x => x.Comment);
			foreach (var item in group)
			{
				if (item.Count() == 1)
				{
					yield return item.Single();
				}
				else if (item.Count() == 2)
				{
					var transaction = item.Single(x => x.Quantity > 0);
					var fee = item.Single(x => x.Quantity == -1);
					transaction.Fee = fee.UnitPrice;
					yield return transaction;
				}
				else
				{
					throw new NotSupportedException();
				}
			}
		}
	}
}
