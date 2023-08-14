using CsvHelper.Configuration;
using GhostfolioSidekick.Ghostfolio.API;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GhostfolioSidekick.FileImporter.DeGiro
{
	public class DeGiroParser : RecordBaseImporter<DeGiroRecord>
	{
		public DeGiroParser(IGhostfolioAPI api) : base(api)
		{
		}

		protected override async Task<Order?> ConvertOrder(DeGiroRecord record, Account account, IEnumerable<DeGiroRecord> allRecords)
		{
			var orderType = GetOrderType(record);
			if (orderType == null)
			{
				return null;
			}

			var asset = await api.FindSymbolByISIN(record.ISIN);
			var fee = GetFee(record, allRecords);

			var order = new Order
			{
				AccountId = account.Id,
				Asset = asset,
				Currency = record.Mutatie,
				Date = record.Datum.ToDateTime(record.Tijd),
				Comment = $"Transaction Reference: [{record.OrderId}]",
				Fee = Math.Abs(fee?.Item1 ?? 0),
				FeeCurrency = fee?.Item2 ?? record.Mutatie,
				Quantity = GetQuantity(record),
				Type = orderType.Value,
				UnitPrice = GetUnitPrice(record),
				ReferenceCode = record.OrderId,
			};

			return order;
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

		private Tuple<decimal?, string>? GetFee(DeGiroRecord record, IEnumerable<DeGiroRecord> allRecords)
		{
			var feeRecord = allRecords.SingleOrDefault(x => x.OrderId == record.OrderId && x != record);
			if (feeRecord == null)
			{
				return null;
			}

			return Tuple.Create(feeRecord.Total, feeRecord.Mutatie);
		}

		private OrderType? GetOrderType(DeGiroRecord record)
		{
			if (record.Omschrijving.Contains("Koop"))
			{
				return OrderType.BUY;
			}

			// TODO, implement other options
			return null;
		}

		private decimal GetQuantity(DeGiroRecord record)
		{
			var quantity = Regex.Match(record.Omschrijving, $"Koop (?<amount>\\d+) @ (?<price>.*) EUR").Groups[1].Value;
			return decimal.Parse(quantity, GetCultureForParsingNumbers());
		}

		private decimal GetUnitPrice(DeGiroRecord record)
		{
			var quantity = Regex.Match(record.Omschrijving, $"Koop (?<amount>\\d+) @ (?<price>.*) EUR").Groups[2].Value;
			return decimal.Parse(quantity, GetCultureForParsingNumbers());
		}

		private CultureInfo GetCultureForParsingNumbers()
		{
			return new CultureInfo("en")
			{
				NumberFormat =
				{
					NumberDecimalSeparator = ","
				}
			};
		}
	}
}
