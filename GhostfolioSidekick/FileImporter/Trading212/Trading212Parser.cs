using CsvHelper.Configuration;
using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.Model;
using System.Globalization;

namespace GhostfolioSidekick.FileImporter.Trading212
{
	public class Trading212Parser : RecordBaseImporter<Trading212Record>
	{
		public Trading212Parser(IGhostfolioAPI api) : base(api)
		{
		}

		protected override async Task<IEnumerable<Activity>> ConvertOrders(Trading212Record record, Account account, IEnumerable<Trading212Record> allRecords)
		{
			var activityType = GetOrderType(record);
			if (activityType == null)
			{
				return Array.Empty<Activity>();
			}

			var asset = string.IsNullOrWhiteSpace(record.ISIN) ? null : await api.FindSymbolByIdentifier(
				record.ISIN,
				!string.IsNullOrWhiteSpace(record.Currency) ? CurrencyHelper.ParseCurrency(record.Currency) : account.Balance.Currency,
				DefaultSetsOfAssetClasses.StockBrokerDefaultSetAssestClasses,
				DefaultSetsOfAssetClasses.StockBrokerDefaultSetAssetSubClasses);

			if (string.IsNullOrWhiteSpace(record.Id))
			{
				record.Id = $"{activityType}_{record.ISIN}_{record.Time.ToInvariantDateOnlyString()}";
			}

			var fees = GetFees(record);

			if (activityType == ActivityType.Convert)
			{
				var parsed = ParserConvertion(record);
				var activitySource = new Activity(
					ActivityType.CashWithdrawal,
					asset,
					record.Time,
					1,
					parsed.Source,
					null,
					$"Transaction Reference: [{record.Id}_SourceCurrencyConversion]",
					record.Id + "_SourceCurrencyConversion"
					);
				var activityTarget = new Activity(
					ActivityType.CashDeposit,
					asset,
					record.Time,
					1,
					parsed.Target,
					null,
					$"Transaction Reference: [{record.Id}_TargetCurrencyConversion]",
					record.Id + "_TargetCurrencyConversion"
				);

				return new[] { activitySource, activityTarget };
			}
			else if (activityType == ActivityType.CashDeposit ||
				activityType == ActivityType.CashWithdrawal ||
				activityType == ActivityType.Interest)
			{
				var currency = string.IsNullOrWhiteSpace(record.Currency) ? record.CurrencyTotal : record.Currency;
				var activity = new Activity(
					activityType.Value,
					asset,
					record.Time,
					1,
					new Money(currency!, record.Total.GetValueOrDefault(0), record.Time),
					fees,
					TransactionReferenceUtilities.GetComment(record.Id),
					record.Id
					);
				return new[] { activity };
			}
			else
			{
				var activity = new Activity(
					activityType.Value,
					asset,
					record.Time,
					record.NumberOfShares!.Value,
					new Money(record.Currency!, record.Price!.Value, record.Time),
					fees,
					TransactionReferenceUtilities.GetComment(record.Id, record.ISIN),
					record.Id
					);
				return new[] { activity };
			}
		}

		private (Money Source, Money Target) ParserConvertion(Trading212Record record)
		{
			// "0.01 GBP -> 0.01 EUR"
			var note = record.Notes;

			if (string.IsNullOrWhiteSpace(note))
			{
				throw new NotSupportedException("Conversion without Notes");
			}

			var splitted = note.Split(' ');

			Money source = new(splitted[1], decimal.Parse(splitted[0], CultureInfo.InvariantCulture), record.Time);
			Money target = new(splitted[4], decimal.Parse(splitted[3], CultureInfo.InvariantCulture), record.Time);

			return (source, target);
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

		private IEnumerable<Money> GetFees(Trading212Record record)
		{
			List<Money> taxes = new();
			if (record.FeeUK != null)
			{
				taxes.Add(new Money(record.FeeUKCurrency!, record.FeeUK.Value, record.Time));
			}

			if (record.FeeFrance != null)
			{
				taxes.Add(new Money(record.FeeFranceCurrency!, record.FeeFrance.Value, record.Time));
			}

			if (record.ConversionFee != null)
			{
				taxes.Add(new Money(record.ConversionFeeCurrency!, record.ConversionFee.Value, record.Time));
			}

			return taxes;
		}

		private ActivityType? GetOrderType(Trading212Record record)
		{
			return record.Action switch
			{
				"Deposit" => ActivityType.CashDeposit,
				"Withdrawal" => ActivityType.CashWithdrawal,
				"Interest on cash" => ActivityType.Interest,
				"Currency conversion" => ActivityType.Convert,
				"Market buy" => ActivityType.Buy,
				"Market sell" => ActivityType.Sell,
				string d when d.Contains("Dividend") => ActivityType.Dividend,
				_ => throw new NotSupportedException(),
			};
		}
	}
}
