namespace GhostfolioSidekick.Model
{
	public class Account
	{
		public Account(string id, string name, Balance balance, List<Activity> activities)
		{
			if (string.IsNullOrEmpty(id))
			{
				throw new ArgumentException($"'{nameof(id)}' cannot be null or empty.", nameof(id));
			}

			if (string.IsNullOrEmpty(name))
			{
				throw new ArgumentException($"'{nameof(name)}' cannot be null or empty.", nameof(name));
			}

			Id = id;
			Name = name;
			Balance = balance ?? throw new ArgumentNullException(nameof(balance));
			Activities = activities ?? throw new ArgumentNullException(nameof(activities));
		}

		public string Name { get; set; }

		public string Id { get; set; }

		public Balance Balance { get; set; }

		public List<Activity> Activities { get; set; }

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