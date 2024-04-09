namespace GhostfolioSidekick.Model.Strategies
{
	public enum StrategiesPriority
	{
		DeterminePrice = 0,

		StockSplit = 1,

		StakeRewardWorkaround = 2,

		SendAndReceiveToBuyAndSell = 3,

		Rounding = 4,

		ApplyDustCorrection = 5,
	}
}