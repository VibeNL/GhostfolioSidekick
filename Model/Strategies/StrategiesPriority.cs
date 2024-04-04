namespace GhostfolioSidekick.Model.Strategies
{
	public enum StrategiesPriority
	{
		DeterminePrice = 0,

		StockSplit = 1,

		StakeRewardWorkaround = 2,

		SendAndReceiveToBuyAndSell = 3,

		ApplyDustCorrection = 4,

		Rounding = 5,
	}
}