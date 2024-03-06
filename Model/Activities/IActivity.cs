using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Compare;

namespace GhostfolioSidekick.Model.Activities
{
	public interface IActivity
	{
		public Account Account { get; }

		public DateTime Date { get; }

		public string? TransactionId { get; }

		public int? SortingPriority { get; }

		public string? Id { get; }

		public string? Description { get; }

		public Task<bool> AreEqual(IExchangeRateService exchangeRateService, IActivity otherActivity);
	}
}
