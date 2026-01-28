using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Performance;

namespace GhostfolioSidekick.Model.Accounts
{
	public class Account
	{
		public Account()
		{
			// EF Core
			Name = null!;
			Balance = null!;
		}

		public Account(string name)
		{
			Name = name;
		}

		public string Name { get; set; }

		public virtual List<Balance> Balance { get; set; } = [];

		public int Id { get; set; }

		public string? Comment { get; set; }

		public virtual Platform? Platform { get; set; }

		public bool SyncActivities { get; set; } = true;

		public bool SyncBalance { get; set; } = true;

		public virtual List<Activity> Activities { get; set; } = [];

		public override string ToString()
		{
			return Name;
		}
	}
}
