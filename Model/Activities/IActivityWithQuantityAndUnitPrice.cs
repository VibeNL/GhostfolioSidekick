namespace GhostfolioSidekick.Model.Activities
{
	public interface IActivityWithQuantityAndUnitPrice : IActivity
	{
		decimal Quantity { get; set; }

		Money? UnitPrice { get; set; }
	}
}
