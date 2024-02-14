using CsvHelper.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.NIBC
{
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Name of bank")]
	public class NIBCParser : RecordBaseImporter<NIBCRecord>
	{
		private readonly ICurrencyMapper currencyMapper;

		public NIBCParser(ICurrencyMapper currencyMapper)
		{
			this.currencyMapper = currencyMapper;
		}

		protected override IEnumerable<PartialActivity> ParseRow(NIBCRecord record, int rowNumber)
		{
			var currency = currencyMapper.Map(record.Currency);
			if (record.Description == "Inkomende overboeking")
			{
				return [PartialActivity.CreateCashDeposit(currency, record.Date, Math.Abs(record.Amount), record.TransactionID)];
			}

			if (record.Description == "Uitgaande overboeking")
			{
				return [PartialActivity.CreateCashWithdrawal(currency, record.Date, Math.Abs(record.Amount), record.TransactionID)];
			}

			if (record.Description == "Renteuitkering" || record.Description == "Bonusrente")
			{
				return [PartialActivity.CreateInterest(currency, record.Date, Math.Abs(record.Amount), record.Description, record.TransactionID)];
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
