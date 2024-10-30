namespace GhostfolioSidekick.Activities.Strategies
{
	public enum StrategiesPriority
	{
		SetInitialValue,

		DeterminePrice,

		StockSplit,

		StakeRewardWorkaround,

		NotNativeSupportedTransactionsInGhostfolio,

		TaxesOnDividends,

		Rounding,

		ApplyDustCorrection,
	}
}