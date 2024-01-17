using CsvHelper.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.NIBC
{
	public class NIBCParser : RecordBaseImporter<NIBCRecord>
	{
		public NIBCParser()
		{
		}

		protected override IEnumerable<PartialActivity> ParseRow(NIBCRecord record, int rowNumber)
		{
			var currency = new Currency(record.Currency);
			if (record.Description == "Inkomende overboeking")
			{
				return [PartialActivity.CreateCashDeposit(currency, record.Date, Math.Abs(record.Amount), null)];
			}

			if (record.Description == "Uitgaande overboeking")
			{
				return [PartialActivity.CreateCashWithdrawal(currency, record.Date, Math.Abs(record.Amount), null)];
			}

			if (record.Description == "Renteuitkering" || record.Description == "Bonusrente")
			{
				return [PartialActivity.CreateInterest(currency, record.Date, Math.Abs(record.Amount), null)];
			}

			return [];
		}

		protected override CsvConfiguration GetConfig()
		{
			return new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				HasHeaderRecord = true,
				CacheFields = true,
				Delimiter = ";",
				ShouldSkipRecord = (r) => r.Row[0]!.StartsWith("Nr v/d rekening"),
			};
		}
	}
}
