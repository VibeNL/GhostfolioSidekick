using CsvHelper.Configuration;
using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.Model;
using System.Globalization;

namespace GhostfolioSidekick.FileImporter.NIBC
{
	public class NIBCParser : RecordBaseImporter<NIBCRecord>
	{
		public NIBCParser(IGhostfolioAPI api) : base(api)
		{
		}

		protected override async Task<IEnumerable<Activity>> ConvertOrders(NIBCRecord record, IEnumerable<NIBCRecord> allRecords, Balance accountBalance)
		{
			var activityType = GetActivityType(record);

			if (activityType == null)
			{
				return [];
			}

			var id = record.TransactionID + (record.Description == "Bonusrente" ? "Bonus" : string.Empty);

			var order = new Activity(
				activityType.Value,
				null,
				record.Date,
				1,
				new Money(CurrencyHelper.ParseCurrency(record.Currency), Math.Abs(record.Amount), record.Date),
				null,
				TransactionReferenceUtilities.GetComment(id),
				id
				);

			return new[] { order };
		}

		private ActivityType? GetActivityType(NIBCRecord record)
		{
			if (record.Description == "Inkomende overboeking")
			{
				return ActivityType.CashDeposit;
			}

			if (record.Description == "Uitgaande overboeking")
			{
				return ActivityType.CashWithdrawal;
			}

			if (record.Description == "Renteuitkering" || record.Description == "Bonusrente")
			{
				return ActivityType.Interest;
			}

			return null;
		}

		protected override CsvConfiguration GetConfig()
		{
			return new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				HasHeaderRecord = true,
				CacheFields = true,
				Delimiter = ";",
				ShouldSkipRecord = (r) => r.Row[0].StartsWith("Nr v/d rekening"),
			};
		}
	}
}
