﻿using CsvHelper.Configuration;
using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.Model;
using System.Globalization;

namespace GhostfolioSidekick.FileImporter.Bunq
{
	public class BunqParser : RecordBaseImporter<BunqRecord>
	{
		public BunqParser(IGhostfolioAPI api) : base(api)
		{
		}

		protected override async Task<IEnumerable<Activity>> ConvertOrders(BunqRecord record, IEnumerable<BunqRecord> allRecords, Balance accountBalance)
		{
			var activityType = GetActivityType(record);

			var id = $"{activityType}{ConvertRowNumber(record, allRecords)}_{record.Date.ToInvariantDateOnlyString()}";

			var order = new Activity(
				activityType,
				null,
				record.Date,
				1,
				new Money(CurrencyHelper.ParseCurrency("EUR"), Math.Abs(record.Amount), record.Date),
				null,
				TransactionReferenceUtilities.GetComment(id),
				id
				);

			return Task.FromResult<IEnumerable<Model.Activity>>(new[] { order });
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

		private ActivityType GetActivityType(BunqRecord record)
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
