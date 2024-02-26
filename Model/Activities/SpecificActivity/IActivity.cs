using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.Model.Activities.SpecificActivity
{
	internal interface IActivity
	{
		public Account Account { get; }

		public DateTime Date { get; }

		public string? TransactionId { get; }

		public int? SortingPriority { get; }

		public string? Id { get; }
	}
}
