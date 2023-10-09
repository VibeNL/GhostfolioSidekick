using CsvHelper.Configuration;
using GhostfolioSidekick.Ghostfolio.API;
using System.Globalization;

namespace GhostfolioSidekick.FileImporter.Bunq
{
	public class BunqParser : RecordBaseImporter<BunqRecord>
	{
		public BunqParser(IGhostfolioAPI api) : base(api)
		{
		}

		protected override async Task<IEnumerable<Model.Activity>> ConvertOrders(BunqRecord record, Model.Account account, IEnumerable<BunqRecord> allRecords)
		{
			var activityType = GetActivityType(record);

			var id = $"{activityType}{ConvertRowNumber(record, allRecords)}_{record.Date:yyyy-MM-dd}";

			var order = new Model.Activity(
				activityType,
				null,
				record.Date,
				1,
				new Model.Money(CurrencyHelper.ParseCurrency("EUR"), Math.Abs(record.Amount), record.Date),
				null,
				$"Transaction Reference: [{id}]",
				id
				);

			return new[] { order };
		}

		private string ConvertRowNumber(BunqRecord record, IEnumerable<BunqRecord> allRecords)
		{
			var groupedByDate = allRecords.GroupBy(x => x.Date);
			IGrouping<DateTime, BunqRecord> group = groupedByDate.Single(x => x.Key == record.Date);
			if (group.Count() == 1)
			{
				return string.Empty;
			}

			var sortedByRow = group.OrderBy(x => x.RowNumber).Select((x, i) => new { x, i }).ToList();
			return (sortedByRow.Single(x => x.x == record).i + 1).ToString();
		}

		private Model.ActivityType GetActivityType(BunqRecord record)
		{
			if (record.Name == "bunq" && record.Description.Contains("bunq Payday"))
			{
				return Model.ActivityType.Interest;
			}

			return record.Amount >= 0 ? Model.ActivityType.CashDeposit : Model.ActivityType.CashWithdrawal;
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
