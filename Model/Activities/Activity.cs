using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.Model.Activities
{
	public class Activity(
		Account account,
		ActivityType activityType,
		DateTime dateTime,
		decimal quantity,
		Money unitPrice,
		string? transactionId)
	{
		public Account Account { get; } = account;

		public ActivityType ActivityType { get; } = activityType;

		public DateTime Date { get; set; } = dateTime;

		public IEnumerable<Money> Fees { get; set; } = [];

		public decimal Quantity { get; set; } = quantity;

		public Money UnitPrice { get; set; } = unitPrice;

		public IEnumerable<Money> Taxes { get; set; } = [];

		public string? TransactionId { get; set; } = transactionId;
		public int? SortingPriority { get; set; }

		public override string ToString()
		{
			return $"{Account}_{ActivityType}_{Date}";
		}
	}
}