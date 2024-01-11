using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model
{
	public class Account
	{
		[SetsRequiredMembers]
		public Account(string id, string name, Balance balance, string? comment, string? platform, List<Activity> activities)
		{
			if (string.IsNullOrWhiteSpace(id))
			{
				throw new ArgumentException($"'{nameof(id)}' cannot be null or whitespace.", nameof(id));
			}

			if (string.IsNullOrWhiteSpace(name))
			{
				throw new ArgumentException($"'{nameof(name)}' cannot be null or whitespace.", nameof(name));
			}

			Id = id;
			Name = name;
			Balance = balance ?? throw new ArgumentNullException(nameof(balance));
			Comment = comment;
			Platform = platform;
			Activities = activities ?? throw new ArgumentNullException(nameof(activities));
		}

		public required string Name { get; set; }

		public required string Id { get; set; }

		public Balance Balance { get; set; }

		public List<Activity> Activities { get; set; }

		public string? Comment { get; }

		public string? Platform { get; }

		internal void ReplaceActivities(ICollection<Activity> newSet)
		{
			Activities.Clear();
			Activities.AddRange(newSet.Where(FilterActivities));
			Balance.Calculate(newSet);
		}

		private bool FilterActivities(Activity activity)
		{
			switch (activity.ActivityType)
			{
				case ActivityType.Buy:
				case ActivityType.Sell:
				case ActivityType.Dividend:
				case ActivityType.Send:
				case ActivityType.Receive:
				case ActivityType.Interest:
				case ActivityType.Gift:
				case ActivityType.LearningReward:
				case ActivityType.StakingReward:
				case ActivityType.Convert:
				case ActivityType.Fee:
					return true;
				// Not needed at the time, is in the balance
				case ActivityType.CashDeposit:
				case ActivityType.CashWithdrawal:
					return false;
				default:
					throw new NotSupportedException();
			}
		}
	}
}