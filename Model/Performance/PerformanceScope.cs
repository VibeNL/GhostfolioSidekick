namespace GhostfolioSidekick.Model.Performance
{
	/// <summary>
	/// Defines the scope of performance calculation
	/// </summary>
	public enum PerformanceScope
	{
		/// <summary>
		/// Entire portfolio performance
		/// </summary>
		Portfolio,

		/// <summary>
		/// Performance for a specific account
		/// </summary>
		Account,

		/// <summary>
		/// Performance for a specific asset/symbol
		/// </summary>
		Asset
	}
}