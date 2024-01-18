namespace GhostfolioSidekick.Model.Activities
{
	public class Activity(
		ActivityType activityType,
		DateTime dateTime,
		decimal quantity,
		Money unitPrice)
	{
		public ActivityType ActivityType { get; } = activityType;

		public DateTime Date { get; set; } = dateTime;

		public IEnumerable<Money> Fees { get; set; } = [];

		public decimal Quantity { get; set; } = quantity;

		public Money UnitPrice { get; set; } = unitPrice;

		public IEnumerable<Money> Taxes { get; set; } = [];
	}
}