using CsvHelper.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.Bunq
{
	public class BunqParser : RecordBaseImporter<BunqRecord>
	{
		public BunqParser()
		{
		}

		protected override IEnumerable<PartialActivity> ParseRow(BunqRecord record, int rowNumber)
		{
			var transactionId = $"{record.Date.ToInvariantString()}_{rowNumber}";
			var currency = Currency.EUR;
			if (record.Name == "bunq" && record.Description.Contains("bunq Payday"))
			{
				return [PartialActivity.CreateInterest(currency, record.Date, Math.Abs(record.Amount), record.Description, new Money(currency, Math.Abs(record.Amount)), transactionId)];
			}

			return record.Amount >= 0 ?
				[PartialActivity.CreateCashDeposit(currency, record.Date, Math.Abs(record.Amount), new Money(currency, Math.Abs(record.Amount)), transactionId)] :
				[PartialActivity.CreateCashWithdrawal(currency, record.Date, Math.Abs(record.Amount), new Money(currency, Math.Abs(record.Amount)), transactionId)];
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
