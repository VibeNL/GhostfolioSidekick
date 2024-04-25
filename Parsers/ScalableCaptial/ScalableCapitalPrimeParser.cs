using CsvHelper.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.ScalableCaptial
{
	public class ScalableCapitalPrimeParser : RecordBaseImporter<ScalableCapitalPrimeRecord>
	{
		private readonly ICurrencyMapper currencyMapper;

		public ScalableCapitalPrimeParser(ICurrencyMapper currencyMapper)
		{
			this.currencyMapper = currencyMapper;
		}

		protected override IEnumerable<PartialActivity> ParseRow(ScalableCapitalPrimeRecord record, int rowNumber)
		{
			if (record.Status != "Executed")
			{
				yield break;
			}

			var currency = currencyMapper.Map(record.Currency);
			var dateTime = DateTime.SpecifyKind(record.Date.ToDateTime(record.Time), DateTimeKind.Utc);
			
			switch (record.Type)
			{
				case "Buy":
				case "Savings plan":
					yield return PartialActivity.CreateBuy(currency, dateTime, [PartialSymbolIdentifier.CreateStockAndETF(record.Isin)], record.Shares!.Value, record.Price!.Value, new Money(currency, Math.Abs(record.Amount)), record.Reference);
					break;
				case "Sell":
					yield return PartialActivity.CreateSell(currency, dateTime, [PartialSymbolIdentifier.CreateStockAndETF(record.Isin)], record.Shares!.Value, record.Price!.Value, new Money(currency, Math.Abs(record.Amount)), record.Reference);
					break;
				case "Distribution":
					yield return PartialActivity.CreateDividend(currency, dateTime, [PartialSymbolIdentifier.CreateStockAndETF(record.Isin)], Math.Abs(record.Amount), new Money(currency, Math.Abs(record.Amount)), record.Reference);
					break;
				case "Deposit":
					yield return PartialActivity.CreateCashDeposit(currency, dateTime, Math.Abs(record.Amount), new Money(currency, Math.Abs(record.Amount)), record.Reference);
					break;
				case "Withdrawal":
					yield return PartialActivity.CreateCashWithdrawal(currency, dateTime, Math.Abs(record.Amount), new Money(currency, Math.Abs(record.Amount)), record.Reference);
					break;
				default:
					yield break;
			}

			if (record.Tax.GetValueOrDefault() != 0)
			{
				yield return PartialActivity.CreateTax(currency, dateTime, record.Tax!.Value, new Money(currency, record.Tax!.Value), record.Reference);
			}

			if (record.Fee.GetValueOrDefault() != 0)
			{
				yield return PartialActivity.CreateFee(currency, dateTime, record.Fee!.Value, new Money(currency, record.Fee!.Value), record.Reference);
			}

		}

		protected override CsvConfiguration GetConfig()
		{
			return new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				HasHeaderRecord = true,
				CacheFields = true,
				Delimiter = ";",
			};
		}
	}
}
