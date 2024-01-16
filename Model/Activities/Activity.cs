using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.Model.Activities
{
	public class Activity(
		ActivityType activityType,
		SymbolProfile? asset,
		DateTime dateTime,
		decimal quantity,
		Money unitPrice)
	{
		public ActivityType ActivityType { get; } = activityType;

		public SymbolProfile? Asset { get; } = asset;

		public DateTime Date { get; set; } = dateTime;

		public IEnumerable<Money> Fees { get; set; } = [];

		public decimal Quantity { get; set; } = quantity;

		public Money UnitPrice { get; set; } = unitPrice;

		public Money? Taxes { get; set; }
	}
}