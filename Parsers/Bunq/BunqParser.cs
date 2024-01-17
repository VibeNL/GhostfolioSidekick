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

		protected override async Task<IEnumerable<PartialActivity>> ParseRow(BunqRecord record, int rowNumber)
		{
			var currency = new Currency("EUR");
			if (record.Name == "bunq" && record.Description.Contains("bunq Payday"))
			{
				return [PartialActivity.CreateInterest(currency, Math.Abs(record.Amount), record.Date)];
			}

			return record.Amount >= 0 ?
				[PartialActivity.CreateCashDeposit(currency, Math.Abs(record.Amount), record.Date)] :
				[PartialActivity.CreateCashWithdrawal(currency, Math.Abs(record.Amount), record.Date)];
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
