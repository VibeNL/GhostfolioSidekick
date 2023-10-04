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
			Activities.AddRange(newSet.Where(x => x.Asset != null));
			Balance.Calculate(newSet);
		}
	}
}