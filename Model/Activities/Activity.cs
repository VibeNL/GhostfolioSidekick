using GhostfolioSidekick.Model.Accounts;
using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Activities
{
	public record class Activity
	{
		public Activity(
		Account account,
		ActivityType activityType,
		DateTime dateTime,
		decimal quantity,
		Money unitPrice,
		string? transactionId)
		{
			Account = account;
			ActivityType = activityType;
			Date = dateTime;
			Quantity = quantity;
			UnitPrice = unitPrice;
			TransactionId = transactionId;
		}

		public Account Account { get; }

		public ActivityType ActivityType { get; }
		
		public DateTime Date { get; }

		public IEnumerable<Money> Fees { get; set; } = [];

		public decimal Quantity { get; set; }

		public Money UnitPrice { get; set; }

		public IEnumerable<Money> Taxes { get; set; } = [];

		public string? TransactionId { get; set; }

		public int? SortingPriority { get; set; }

		public string? Description { get; set; }

		public string? Id { get; set; }

		[ExcludeFromCodeCoverage]
		public override string ToString()
		{
			return $"{Account}_{ActivityType}_{Date}";
		}
	}
}