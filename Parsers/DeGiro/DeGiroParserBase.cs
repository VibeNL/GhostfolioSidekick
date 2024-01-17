using CsvHelper.Configuration;
using GhostfolioSidekick.Ghostfolio.API;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.DeGiro
{
	public abstract class DeGiroParserBase<T> : RecordBaseImporter<T> where T : DeGiroRecordBase
	{
		public DeGiroParserBase(IGhostfolioAPI api) : base(api)
		{
		}

		protected override async Task<IEnumerable<Activity>> ConvertOrders(T record, Account account, IEnumerable<T> allRecords)
		{
			account.Balance.SetKnownBalance(new Money(CurrencyHelper.ParseCurrency(record.BalanceCurrency), record.Balance, record.Date.ToDateTime(record.Time)));

			var activityType = record.GetActivityType();
			if (activityType == null || activityType == ActivityType.Undefined)
			{
				return Array.Empty<Activity>();
			}

			var asset = string.IsNullOrWhiteSpace(record.ISIN) ? null : await api.FindSymbolByIdentifier(
				record.ISIN,
				CurrencyHelper.ParseCurrency(record.Mutation) ?? account.Balance.Currency,
				DefaultSetsOfAssetClasses.StockBrokerDefaultSetAssestClasses,
				DefaultSetsOfAssetClasses.StockBrokerDefaultSetAssetSubClasses
			);

			var orderNumber = allRecords.Where(x => x.TransactionId == record.TransactionId).Where(x => x.GetActivityType() == activityType).ToList().IndexOf(record);

			var fees = GetFee(record, allRecords);
			var taxes = GetTaxes(record, allRecords);

			if (string.IsNullOrWhiteSpace(record.TransactionId))
			{
				record.TransactionId = $"{activityType}_{record.Date.ToInvariantString()}_{record.Time.ToInvariantString()}_{record.ISIN}";
			}

			Activity activity;
			if (activityType == ActivityType.CashDeposit ||
				activityType == ActivityType.CashWithdrawal ||
				activityType == ActivityType.Interest ||
				activityType == ActivityType.Fee)
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
					new Money(CurrencyHelper.ParseCurrency(record.Mutation), record.Total.GetValueOrDefault() - (taxes?.Amount ?? 0), record.Date.ToDateTime(record.Time)),
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
					record.GetQuantity(),
					new Money(CurrencyHelper.ParseCurrency(record.Mutation), record.GetUnitPrice(), record.Date.ToDateTime(record.Time)),
					orderNumber == 0 ? fees : [],
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

		protected IEnumerable<Money> GetFee(DeGiroRecordBase record, IEnumerable<DeGiroRecordBase> allRecords)
		{
			// Costs of stocks.
			var feeRecords = allRecords.Where(x => !string.IsNullOrWhiteSpace(x.TransactionId) && x.TransactionId == record.TransactionId && x.IsFee());
			return feeRecords.Where(x => x.Total != null).Select(x => new Money(CurrencyHelper.ParseCurrency(x.Mutation), x.Total!.Value * -1, x.Date.ToDateTime(x.Time)));
		}

		protected Money? GetTaxes(DeGiroRecordBase record, IEnumerable<DeGiroRecordBase> allRecords)
		{
			// Taxes of dividends
			var feeRecord = allRecords.SingleOrDefault(x => x.Date == record.Date && x.ISIN == record.ISIN && x.IsTaxes());
			if (feeRecord != null)
			{
				return new Money(CurrencyHelper.ParseCurrency(feeRecord.Mutation), feeRecord.Total!.Value * -1, feeRecord.Date.ToDateTime(feeRecord.Time));
			}

			return null;
		}
	}
}