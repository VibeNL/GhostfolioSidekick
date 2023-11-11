using CsvHelper.Configuration;
using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.Model;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GhostfolioSidekick.FileImporter.DeGiro
{
	public class DeGiroParser : RecordBaseImporter<DeGiroRecord>
	{
		public DeGiroParser(IGhostfolioAPI api) : base(api)
		{
		}

		protected override async Task<IEnumerable<Activity>> ConvertOrders(DeGiroRecord record, Account account, IEnumerable<DeGiroRecord> allRecords)
		{
			account.Balance.SetKnownBalance(new Money(CurrencyHelper.ParseCurrency(record.Saldo), record.SaldoValue, record.Datum.ToDateTime(record.Tijd)));

			var activityType = GetActivityType(record);
			if (activityType == null)
			{
				return Array.Empty<Activity>();
			}

			var asset = string.IsNullOrWhiteSpace(record.ISIN) ? null : await api.FindSymbolByIdentifier(
				record.ISIN,
				CurrencyHelper.ParseCurrency(record.Mutatie) ?? account.Balance.Currency,
				DefaultSetsOfAssetClasses.StockBrokerDefaultSetAssestClasses,
				DefaultSetsOfAssetClasses.StockBrokerDefaultSetAssetSubClasses
			);

			var orderNumber = allRecords.Where(x => x.OrderId == record.OrderId).Where(x => GetActivityType(x) == activityType).ToList().IndexOf(record);

			var fee = GetFee(record, allRecords);
			var taxes = GetTaxes(record, allRecords);

			if (string.IsNullOrWhiteSpace(record.OrderId))
			{
				record.OrderId = $"{activityType}_{record.Datum.ToString("dd-MM-yyyy")}_{record.Tijd}_{record.ISIN}";
			}

			Activity activity;
			if (activityType == ActivityType.CashDeposit || activityType == ActivityType.CashWithdrawal)
			{
				activity = new Activity(
					activityType.Value,
					null,
					record.Datum.ToDateTime(record.Tijd),
					1,
					new Money(CurrencyHelper.ParseCurrency(record.Mutatie), record.Total.GetValueOrDefault(), record.Datum.ToDateTime(record.Tijd)).Absolute(),
					null,
					$"Transaction Reference: [{activityType}{record.Datum}]",
					$"{activityType}{record.Datum}"
					);
			}
			else if (activityType == ActivityType.Dividend)
			{
				activity = new Activity(
					activityType.Value,
					asset,
					record.Datum.ToDateTime(record.Tijd),
					1,
					new Money(CurrencyHelper.ParseCurrency(record.Mutatie), record.Total.GetValueOrDefault() - (taxes?.Item1 ?? 0), record.Datum.ToDateTime(record.Tijd)),
					null,
					TransactionReferenceUtilities.GetComment(record.OrderId, record.ISIN),
					record.OrderId);
			}
			else
			{
				var orderId = record.OrderId + (orderNumber == 0 ? string.Empty : $" {orderNumber + 1}"); // suborders are suffixed with an odernumber

				activity = new Activity(
					activityType.Value,
					asset,
					record.Datum.ToDateTime(record.Tijd),
					GetQuantity(record),
					new Money(CurrencyHelper.ParseCurrency(record.Mutatie), GetUnitPrice(record), record.Datum.ToDateTime(record.Tijd)),
					new Money(CurrencyHelper.ParseCurrency(fee?.Item2 ?? record.Mutatie), Math.Abs(orderNumber == 0 ? (fee?.Item1 ?? 0) : 0), record.Datum.ToDateTime(record.Tijd)),
					TransactionReferenceUtilities.GetComment(orderId, record.ISIN),
					orderId);
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
			var feeRecord = allRecords.SingleOrDefault(x => !string.IsNullOrWhiteSpace(x.OrderId) && x.OrderId == record.OrderId && x.Omschrijving == "DEGIRO Transactiekosten en/of kosten van derden");
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

		private ActivityType? GetActivityType(DeGiroRecord record)
		{
			if (record.Omschrijving.Contains("Verkoop")) // check Verkoop first because otherwise koop get's triggered
			{
				return ActivityType.Sell;
			}

			if (record.Omschrijving.Contains("Koop"))
			{
				return ActivityType.Buy;
			}

			if (record.Omschrijving.Equals("Dividend"))
			{
				return ActivityType.Dividend;
			}

			if (record.Omschrijving.Equals("flatex terugstorting"))
			{
				return ActivityType.CashWithdrawal;
			}

			if (record.Omschrijving.Contains("Deposit") && !record.Omschrijving.Contains("Reservation"))
			{
				return ActivityType.CashDeposit;
			}

			if (record.Omschrijving.Equals("DEGIRO Verrekening Promotie"))
			{
				return ActivityType.CashDeposit; // TODO: Gift?
			}

			// TODO, implement other options
			return null;
		}

		private decimal GetQuantity(DeGiroRecord record)
		{
			// oop is the same for both buy and sell or Koop and Verkoop in dutch
			// dont include currency at the end, this can be other things than EUR
			var quantity = Regex.Match(record.Omschrijving, $"oop (?<amount>\\d+) @ (?<price>[0-9]+,[0-9]+)").Groups[1].Value;

			return decimal.Parse(quantity, GetCultureForParsingNumbers());
		}

		private decimal GetUnitPrice(DeGiroRecord record)
		{
			// oop is the same for both buy and sell or Koop and Verkoop in dutch
			// dont include currency at the end, this can be other things than EUR
			var quantity = Regex.Match(record.Omschrijving, $"oop (?<amount>\\d+) @ (?<price>[0-9]+,[0-9]+)").Groups[2].Value;

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