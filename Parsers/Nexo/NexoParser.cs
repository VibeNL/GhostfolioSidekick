using CsvHelper.Configuration;
using GhostfolioSidekick.Model.Activities;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.Nexo
{
	public class NexoParser : RecordBaseImporter<NexoRecord>
	{
		public NexoParser()
		{
		}

		protected override IEnumerable<PartialActivity> ParseRow(NexoRecord record, int rowNumber)
		{
			if (!record.Details.StartsWith("approved"))
			{
				yield break;
			}

			switch (record.Type)
			{
				case "Top up Crypto":
				case "Exchange Cashback":
				case "Referral Bonus": // TODO: Should be a 'reward'
				case "Deposit":
				//return new[] { SetActivity(outputActivity, ActivityType.Receive) };
				case "Exchange Deposited On":
				case "Exchange":
				//return HandleConversion(inputActivity, outputActivity, record);
				case "Interest":
				case "Fixed Term Interest":
				//return new[] { SetActivity(outputActivity, outputActivity.Asset == null ? ActivityType.Interest : ActivityType.StakingReward) }; // Staking rewards are not yet supported
				case "Deposit To Exchange":
				case "Locking Term Deposit":
				case "Unlocking Term Deposit":
					yield break;
				default: throw new NotSupportedException($"{record.Type}");
			}
		}

		protected override CsvConfiguration GetConfig()
		{
			return new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				HasHeaderRecord = true,
				CacheFields = true,
				Delimiter = ",",
			};
		}
	}
}
