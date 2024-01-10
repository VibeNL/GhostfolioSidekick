using CsvHelper.Configuration;
using GhostfolioSidekick.Ghostfolio.API;
using System.Globalization;

namespace GhostfolioSidekick.FileImporter.NIBC
{
	public class NIBCParser : RecordBaseImporter<NIBCRecord>
	{
		public NIBCParser(IGhostfolioAPI api) : base(api)
		{
		}

		protected override Task<IEnumerable<Model.Activity>> ConvertOrders(NIBCRecord record, Model.Account account, IEnumerable<NIBCRecord> allRecords)
		{
			var activityType = GetActivityType(record);

			if (activityType == null)
			{
				return Task.FromResult(Enumerable.Empty<Model.Activity>());
			}

			var id = record.TransactionID + (record.Description == "Bonusrente" ? "Bonus" : string.Empty);

			var order = new Model.Activity(
				activityType.Value,
				null,
				record.Date,
				1,
				new Model.Money(CurrencyHelper.ParseCurrency(record.Currency), Math.Abs(record.Amount), record.Date),
				null,
				TransactionReferenceUtilities.GetComment(id),
				id
				);

			return Task.FromResult<IEnumerable<Model.Activity>>(new[] { order });
		}

		private Model.ActivityType? GetActivityType(NIBCRecord record)
		{
			if (record.Description == "Inkomende overboeking")
			{
				return Model.ActivityType.CashDeposit;
			}

			if (record.Description == "Uitgaande overboeking")
			{
				return Model.ActivityType.CashWithdrawal;
			}

			if (record.Description == "Renteuitkering" || record.Description == "Bonusrente")
			{
				return Model.ActivityType.Interest;
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
				ShouldSkipRecord = (r) => r.Row[0]!.StartsWith("Nr v/d rekening"),
			};
		}
	}
}
