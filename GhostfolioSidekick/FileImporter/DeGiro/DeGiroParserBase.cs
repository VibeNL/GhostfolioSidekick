using CsvHelper.Configuration;
using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.Model;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GhostfolioSidekick.FileImporter.DeGiro
{
	public class DeGiroParserBase<T> : RecordBaseImporter<T> where T:DeGiroRecordBase
	{
		public DeGiroParserBase(IGhostfolioAPI api) : base(api)
		{
		}

		protected override async Task<IEnumerable<Activity>> ConvertOrders(T record, Account account, IEnumerable<T> allRecords)
		{
			account.Balance.SetKnownBalance(new Money(CurrencyHelper.ParseCurrency(record.BalanceCurrency), record.Balance, record.Date.ToDateTime(record.Time)));

			var activityType = GetActivityType(record);
			if (activityType == null)
			{
				return Array.Empty<Activity>();
			}

			var asset = string.IsNullOrWhiteSpace(record.ISIN) ? null : await api.FindSymbolByIdentifier(
				record.ISIN,
				CurrencyHelper.ParseCurrency(record.Mutation) ?? account.Balance.Currency,
				DefaultSetsOfAssetClasses.StockBrokerDefaultSetAssestClasses,
				DefaultSetsOfAssetClasses.StockBrokerDefaultSetAssetSubClasses
			);

			var orderNumber = allRecords.Where(x => x.TransactionId == record.TransactionId).Where(x => GetActivityType(x) == activityType).ToList().IndexOf(record);

			var fee = GetFee(record, allRecords);
			var taxes = GetTaxes(record, allRecords);

			if (string.IsNullOrWhiteSpace(record.TransactionId))
			{
				record.TransactionId = $"{activityType}_{record.Date.ToInvariantString()}_{record.Time.ToInvariantString()}_{record.ISIN}";
			}

			Activity activity;
			if (activityType == ActivityType.CashDeposit || activityType == ActivityType.CashWithdrawal)
			{
				activity = new Activity(
					activityType.Value,
					null,
					record.Date.ToDateTime(record.Time),
					1,
					new Money(CurrencyHelper.ParseCurrency(record.Mutation), record.Total.GetValueOrDefault(), record.Date.ToDateTime(record.Time)).Absolute(),
					null,
					$"Transaction Reference: [{activityType}{record.Date.ToInvariantString()}]",
					$"{activityType}{record.Date.ToInvariantString()}"
					);
			}
			else if (activityType == ActivityType.Dividend)
			{
				activity = new Activity(
					activityType.Value,
					asset,
					record.Date.ToDateTime(record.Time),
					1,
					new Money(CurrencyHelper.ParseCurrency(record.Mutation), record.Total.GetValueOrDefault() - (taxes?.Item1 ?? 0), record.Date.ToDateTime(record.Time)),
					null,
					TransactionReferenceUtilities.GetComment(record.TransactionId, record.ISIN),
					record.TransactionId);
			}
			else
			{
				var orderId = record.TransactionId + (orderNumber == 0 ? string.Empty : $" {orderNumber + 1}"); // suborders are suffixed with an odernumber

				activity = new Activity(
					activityType.Value,
					asset,
					record.Date.ToDateTime(record.Time),
					GetQuantity(record),
					new Money(CurrencyHelper.ParseCurrency(record.Mutation), GetUnitPrice(record), record.Date.ToDateTime(record.Time)),
					new[] { new Money(CurrencyHelper.ParseCurrency(fee?.Item2 ?? record.Mutation), Math.Abs(orderNumber == 0 ? (fee?.Item1 ?? 0) : 0), record.Date.ToDateTime(record.Time)) },
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

		private Tuple<decimal?, string>? GetFee(DeGiroRecordBase record, IEnumerable<DeGiroRecordBase> allRecords)
		{
			// Costs of stocks
			var feeRecord = allRecords.SingleOrDefault(x => !string.IsNullOrWhiteSpace(x.TransactionId) && x.TransactionId == record.TransactionId && x.Description == "DEGIRO Transactiekosten en/of kosten van derden");
			if (feeRecord != null)
			{
				return Tuple.Create(feeRecord.Total, feeRecord.Mutation);
			}

			return null;
		}

		private Tuple<decimal?, string>? GetTaxes(DeGiroRecordBase record, IEnumerable<DeGiroRecordBase> allRecords)
		{
			// Taxes of dividends
			var feeRecord = allRecords.SingleOrDefault(x => x.Date == record.Date && x.ISIN == record.ISIN && x.Description == "Dividendbelasting");
			if (feeRecord != null)
			{
				return Tuple.Create(feeRecord.Total * -1, feeRecord.Mutation);
			}

			return null;
		}

		private ActivityType? GetActivityType(DeGiroRecordBase record)
		{
			if (record.Description.Contains("Verkoop")) // check Verkoop first because otherwise koop get's triggered
			{
				return ActivityType.Sell;
			}

			if (record.Description.Contains("Koop"))
			{
				return ActivityType.Buy;
			}

			if (record.Description.Equals("Dividend"))
			{
				return ActivityType.Dividend;
			}

			if (record.Description.Equals("flatex terugstorting"))
			{
				return ActivityType.CashWithdrawal;
			}

			if (record.Description.Contains("Deposit") && !record.Description.Contains("Reservation"))
			{
				return ActivityType.CashDeposit;
			}

			if (record.Description.Equals("DEGIRO Verrekening Promotie"))
			{
				return ActivityType.CashDeposit; // TODO: Gift?
			}

			// TODO, implement other options
			return null;
		}

		private decimal GetQuantity(DeGiroRecordBase record)
		{
			// oop is the same for both buy and sell or Koop and Verkoop in dutch
			// dont include currency at the end, this can be other things than EUR
			var quantity = Regex.Match(record.Description, $"oop (?<amount>\\d+) @ (?<price>[0-9]+,[0-9]+)").Groups[1].Value;

			return decimal.Parse(quantity, GetCultureForParsingNumbers());
		}

		private decimal GetUnitPrice(DeGiroRecordBase record)
		{
			// oop is the same for both buy and sell or Koop and Verkoop in dutch
			// dont include currency at the end, this can be other things than EUR
			var quantity = Regex.Match(record.Description, $"oop (?<amount>\\d+) @ (?<price>[0-9]+,[0-9]+)").Groups[2].Value;

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