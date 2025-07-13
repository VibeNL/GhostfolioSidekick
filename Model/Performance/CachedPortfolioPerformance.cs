using GhostfolioSidekick.Model.Portfolio;

namespace GhostfolioSidekick.Model.Performance
{
	/// <summary>
	/// Represents stored portfolio performance calculation results
	/// </summary>
	public class PortfolioPerformanceSnapshot
	{
		public int Id { get; set; }

		/// <summary>
		/// Hash of the portfolio composition (holdings + activities) to detect changes
		/// </summary>
		public string PortfolioHash { get; set; } = string.Empty;

		/// <summary>
		/// Start date of the performance calculation period
		/// </summary>
		public DateTime StartDate { get; set; }

		/// <summary>
		/// End date of the performance calculation period
		/// </summary>
		public DateTime EndDate { get; set; }

		/// <summary>
		/// Base currency for all calculations
		/// </summary>
		public Currency BaseCurrency { get; set; } = Currency.EUR;

		/// <summary>
		/// Type of calculation used (Basic, Enhanced, MarketData)
		/// </summary>
		public string CalculationType { get; set; } = string.Empty;

		/// <summary>
		/// The calculated performance result
		/// </summary>
		public PortfolioPerformance Performance { get; set; } = new();

		/// <summary>
		/// When this calculation was performed
		/// </summary>
		public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

		/// <summary>
		 /// Version number for tracking updates to the same period
		 /// </summary>
		public int Version { get; set; } = 1;

		/// <summary>
		/// Additional metadata as JSON (holdings count, calculation time, etc.)
		/// </summary>
		public string? Metadata { get; set; }

		/// <summary>
		/// Whether this snapshot is the current/latest version for this period
		/// </summary>
		public bool IsLatest { get; set; } = true;
	}
}