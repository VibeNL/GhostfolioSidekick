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

		protected override async Task<IEnumerable<Model.Activity>> ConvertOrders(DeGiroRecord record, Model.Account account, IEnumerable<DeGiroRecord> allRecords)
		{
			var orderType = GetOrderType(record);
			if (orderType == null)
			{
				return Array.Empty<Model.Activity>();
			}

			var asset = await api.FindSymbolByISIN(record.ISIN);
			var fee = GetFee(record, allRecords);
			var taxes = GetTaxes(record, allRecords);

			if (string.IsNullOrWhiteSpace(record.OrderId))
			{
				record.OrderId = $"{orderType}_{record.Datum.ToString("dd-MM-yyyy")}_{record.Tijd}_{record.ISIN}";
			}

			Model.Activity order;
			if (orderType == Model.ActivityType.Dividend)
			{
				order = new Model.Activity(
					orderType.Value,
					asset,
					record.Datum.ToDateTime(record.Tijd),
					1,
					new Model.Money(CurrencyHelper.ParseCurrency(record.Mutatie), record.Total.GetValueOrDefault() - (taxes?.Item1 ?? 0)),
					new Model.Money(CurrencyHelper.ParseCurrency(fee?.Item2 ?? record.Mutatie), Math.Abs(fee?.Item1 ?? 0)),
					$"Transaction Reference: [{record.OrderId}]",
					record.OrderId);
			}
			else
			{
				order = new Model.Activity(
					orderType.Value,
					asset,
					record.Datum.ToDateTime(record.Tijd),
					GetQuantity(record),
					new Model.Money(CurrencyHelper.ParseCurrency(record.Mutatie), GetUnitPrice(record)),
					new Model.Money(CurrencyHelper.ParseCurrency(fee?.Item2 ?? record.Mutatie), Math.Abs(fee?.Item1 ?? 0)),
					$"Transaction Reference: [{record.OrderId}]",
					record.OrderId);
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

		private Model.ActivityType? GetOrderType(DeGiroRecord record)
		{
			if (record.Omschrijving.Contains("Koop"))
			{
				return Model.ActivityType.Buy;
			}

			if (record.Omschrijving.Equals("Dividend"))
			{
				return Model.ActivityType.Dividend;
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
