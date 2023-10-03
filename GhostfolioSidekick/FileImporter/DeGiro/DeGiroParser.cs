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

		protected override async Task<IEnumerable<Order>> ConvertOrders(DeGiroRecord record, Account account, IEnumerable<DeGiroRecord> allRecords)
		{
			var orderType = GetOrderType(record);
			if (orderType == null)
			{
				return Array.Empty<Order>();
			}

			var asset = await api.FindSymbolByISIN(record.ISIN);
			var fee = GetFee(record, allRecords);
			var taxes = GetTaxes(record, allRecords);

			if (string.IsNullOrWhiteSpace(record.OrderId))
			{
				record.OrderId = $"{orderType}_{record.Datum.ToString("dd-MM-yyyy")}_{record.Tijd}_{record.ISIN}";
			}

			Order order;
			if (orderType == OrderType.DIVIDEND)
			{
				order = new Order
				{
					AccountId = account.Id,
					Asset = asset,
					Currency = record.Mutatie,
					Date = record.Datum.ToDateTime(record.Tijd),
					Comment = $"Transaction Reference: [{record.OrderId}]",
					Fee = Math.Abs(fee?.Item1 ?? 0),
					FeeCurrency = fee?.Item2 ?? record.Mutatie,
					Quantity = 1,
					Type = orderType.Value,
					UnitPrice = record.Total.GetValueOrDefault() - taxes.Item1.GetValueOrDefault(),
					ReferenceCode = record.OrderId,
				};
			}
			else
			{
				order = new Order
				{
					AccountId = account.Id,
					Asset = asset,
					Currency = record.Mutatie,
					Date = record.Datum.ToDateTime(record.Tijd),
					Comment = $"Transaction Reference: [{record.OrderId}]",
					Fee = Math.Abs(fee?.Item1 ?? 0),
					FeeCurrency = fee?.Item2 ?? record.Mutatie,
					Quantity = GetQuantity(orderType, record),
					Type = orderType.Value,
					UnitPrice = GetUnitPrice(orderType, record),
					ReferenceCode = record.OrderId,
				};
			}

			return new[] { order };
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
			// Costs of stocks
			var feeRecord = allRecords.SingleOrDefault(x => !string.IsNullOrWhiteSpace(x.OrderId) && x.OrderId == record.OrderId && x != record);
			if (feeRecord != null)
			{
				return Tuple.Create(feeRecord.Total, feeRecord.Mutatie);
			}

			return null;
		}

		private Tuple<decimal?, string>? GetTaxes(DeGiroRecord record, IEnumerable<DeGiroRecord> allRecords)
		{
			// Taxes of dividends
			var feeRecord = allRecords.SingleOrDefault(x => x.Datum == record.Datum && x.ISIN == record.ISIN && x.Omschrijving == "Dividendbelasting");
			if (feeRecord != null)
			{
				return Tuple.Create(feeRecord.Total * -1, feeRecord.Mutatie);
			}

			return null;
		}

		private OrderType? GetOrderType(DeGiroRecord record)
		{
			if (record.Omschrijving.Contains("Koop"))
			{
				return OrderType.BUY;
			}

			if (record.Omschrijving.Equals("Dividend"))
			{
				return OrderType.DIVIDEND;
			}

			// TODO, implement other options
			return null;
		}

		private decimal GetQuantity(OrderType? orderType, DeGiroRecord record)
		{
			var quantity = Regex.Match(record.Omschrijving, $"Koop (?<amount>\\d+) @ (?<price>.*) EUR").Groups[1].Value;
			return decimal.Parse(quantity, GetCultureForParsingNumbers());
		}

		private decimal GetUnitPrice(OrderType? orderType, DeGiroRecord record)
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
