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

			var id = $"{activityType}_{record.Date:yyyy-MM-dd}";

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
