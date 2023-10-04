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
			var activityType = GetOrderType(record);
			if (activityType == null)
			{
				return Array.Empty<Model.Activity>();
			}

			var asset = string.IsNullOrWhiteSpace(record.ISIN) ? null : await api.FindSymbolByISIN(record.ISIN);
			var fee = GetFee(record, allRecords);
			var taxes = GetTaxes(record, allRecords);

			if (string.IsNullOrWhiteSpace(record.OrderId))
			{
				record.OrderId = $"{activityType}_{record.Datum.ToString("dd-MM-yyyy")}_{record.Tijd}_{record.ISIN}";
			}

			Model.Activity activity;
			if (activityType == Model.ActivityType.CashDeposit || activityType == Model.ActivityType.CashWithdrawel)
			{
				activity = new Model.Activity(
					activityType.Value,
					null,
					record.Datum.ToDateTime(record.Tijd),
					1,
					new Model.Money(CurrencyHelper.ParseCurrency(record.Mutatie), record.Total.GetValueOrDefault(), record.Datum.ToDateTime(record.Tijd)),
					null,
					$"Transaction Reference: [{activityType}{record.Datum}]",
					$"{activityType}{record.Datum}"
					);
			}
			else if (activityType == Model.ActivityType.Dividend)
			{
				activity = new Model.Activity(
					activityType.Value,
					asset,
					record.Datum.ToDateTime(record.Tijd),
					1,
					new Model.Money(CurrencyHelper.ParseCurrency(record.Mutatie), record.Total.GetValueOrDefault() - (taxes?.Item1 ?? 0), record.Datum.ToDateTime(record.Tijd)),
					null,
					$"Transaction Reference: [{record.OrderId}]",
					record.OrderId);
			}
			else
			{
				activity = new Model.Activity(
					activityType.Value,
					asset,
					record.Datum.ToDateTime(record.Tijd),
					GetQuantity(record),
					new Model.Money(CurrencyHelper.ParseCurrency(record.Mutatie), GetUnitPrice(record), record.Datum.ToDateTime(record.Tijd)),
					new Model.Money(CurrencyHelper.ParseCurrency(fee?.Item2 ?? record.Mutatie), Math.Abs(fee?.Item1 ?? 0), record.Datum.ToDateTime(record.Tijd)),
					$"Transaction Reference: [{record.OrderId}]",
					record.OrderId);
			}

			return new[] { activity };
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

			if (record.Omschrijving.Contains("Deposit"))
			{
				return Model.ActivityType.CashDeposit;
			}

			if (record.Omschrijving.Equals("Processed Flatex Withdrawal"))
			{
				return Model.ActivityType.CashWithdrawel;
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
