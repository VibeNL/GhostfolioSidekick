using CsvHelper.Configuration;
using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.Model;
using System.Globalization;
using System.Text.RegularExpressions;

namespace GhostfolioSidekick.FileImporter.Coinbase
{
	public class CoinbaseParser : CryptoRecordBaseImporter<CoinbaseRecord>
	{
		public CoinbaseParser(
			IApplicationSettings applicationSettings,
			IGhostfolioAPI api) : base(applicationSettings.ConfigurationInstance, api)
		{
		}

		protected override async Task<IEnumerable<Activity>> ConvertOrders(CoinbaseRecord record, Account account, IEnumerable<CoinbaseRecord> allRecords)
		{
			// Coinbase does not support balance
			account.Balance.SetKnownBalance(new Money(account.Balance.Currency, 0, DateTime.UtcNow));

			var activityType = MapType(record);
			var asset = await GetAsset(record.Asset, account);

			if (asset == null)
			{
				return [];
			}

			var date = record.Timestamp.ToUniversalTime();

			var fees = new List<Money>();

			if (record.Fee != null)
			{
				fees.Add(new Money(record.Currency, record.Fee.GetValueOrDefault(0), date));
			}

			var unitprice = new Money(record.Currency, 0, date);

			if (asset != null)
			{
				unitprice = await GetCorrectUnitPrice(new Money(record.Currency, record.Price ?? 0, date), asset, date);
			}

			var id = $"{activityType}{ConvertRowNumber(record, allRecords)}_{date.ToInvariantDateOnlyString()}";

			if (activityType != ActivityType.Convert)
			{
				var activity = new Activity(
					activityType,
					asset,
					date,
					Math.Abs(record.Quantity),
					unitprice,
					fees,
					TransactionReferenceUtilities.GetComment(id, record.Asset),
					id
					);

				return [activity];
			}

			id = $"{ActivityType.Sell}{ConvertRowNumber(record, allRecords)}_{date.ToInvariantDateOnlyString()}";
			var sellActivity = new Activity(
					ActivityType.Sell,
					asset,
					date,
					Math.Abs(record.Quantity),
					unitprice,
					fees,
					TransactionReferenceUtilities.GetComment(id, record.Asset),
					id
					);

			var result = ParseNote(record.Notes);
			var parseAmount = result.Item1;
			string parsedAsset = result.Item2;
			var asset2 = await GetAsset(parsedAsset, account);

			if (asset2 == null)
			{
				return [];
			}

			var unitprice2 = await GetCorrectUnitPrice(new Money(record.Currency, 0, date), asset2!, date);
			var id2 = $"{ActivityType.Buy}{ConvertRowNumber(record, allRecords)}_{date.ToInvariantDateOnlyString()}";
			var buyActivity = new Activity(
					ActivityType.Buy,
					asset2,
					date,
					Math.Abs(parseAmount),
					unitprice2,
					[],
					TransactionReferenceUtilities.GetComment(id2, parsedAsset),
					id2
					);
			return [sellActivity, buyActivity];
		}

		private (decimal, string) ParseNote(string note)
		{
			// Converted 0.00087766 ETH to 1.629352 USDC
			var match = Regex.Match(note, "Converted ([0-9.,]+) ([A-Za-z0-9]+) to ([0-9.,]+) ([A-Za-z0-9]+)", RegexOptions.IgnoreCase);
			var quantity = match.Groups[3].Value;
			var asset = match.Groups[4].Value;

			var amount = decimal.Parse(quantity, GetCultureForParsingNumbers());

			return (amount, asset);
		}

		private ActivityType MapType(CoinbaseRecord record)
		{
			switch (record.Type)
			{
				case "Buy":
					return ActivityType.Buy;
				case "Sell":
					return ActivityType.Sell;
				case "Receive":
					return ActivityType.Receive;
				case "Send":
					return ActivityType.Send;
				case "Convert":
					return ActivityType.Convert;
				case "Rewards Income":
					return ActivityType.StakingReward;
				case "Learning Reward":
					return ActivityType.LearningReward;
				default:
					throw new NotSupportedException();
			}
		}

		protected override CsvConfiguration GetConfig()
		{
			return new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				HasHeaderRecord = true,
				CacheFields = true,
				Delimiter = ",",
				ShouldSkipRecord = (r) =>
				{
					return !r.Row[0]!.StartsWith("Timestamp") && !r.Row[0]!.StartsWith("20");
				},
			};
		}

		private string ConvertRowNumber(CoinbaseRecord record, IEnumerable<CoinbaseRecord> allRecords)
		{
			var groupedByDate = allRecords.GroupBy(x => x.Timestamp);
			IGrouping<DateTime, CoinbaseRecord> group = groupedByDate.Single(x => x.Key == record.Timestamp);
			if (group.Count() == 1)
			{
				return string.Empty;
			}

			var sortedByRow = group.OrderBy(x => x.RowNumber).Select((x, i) => new { x, i }).ToList();
			return (sortedByRow.Single(x => x.x == record).i + 1).ToString();
		}

		private CultureInfo GetCultureForParsingNumbers()
		{
			return new CultureInfo("en");
		}
	}
}
