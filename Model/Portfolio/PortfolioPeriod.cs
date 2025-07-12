using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Model.Portfolio
{
	/// <summary>
	/// Represents a period snapshot for TWR calculation
	/// </summary>
	public record PortfolioPeriod
	{
		/// <summary>
		/// Start date of the period
		/// </summary>
		public DateTime StartDate { get; init; }

		/// <summary>
		/// End date of the period
		/// </summary>
		public DateTime EndDate { get; init; }

		/// <summary>
		/// Portfolio value at the start of the period
		/// </summary>
		public Money StartValue { get; init; }

		/// <summary>
		/// Portfolio value at the end of the period
		/// </summary>
		public Money EndValue { get; init; }

		/// <summary>
		/// Cash flows during this period (positive for deposits, negative for withdrawals)
		/// </summary>
		public Money CashFlow { get; init; }

		/// <summary>
		/// Activities that occurred during this period
		/// </summary>
		public List<Activity> Activities { get; init; }

		public PortfolioPeriod()
		{
			// EF Core
			StartValue = new Money();
			EndValue = new Money();
			CashFlow = new Money();
			Activities = [];
		}

		public PortfolioPeriod(
			DateTime startDate,
			DateTime endDate,
			Money startValue,
			Money endValue,
			Money cashFlow,
			List<Activity> activities)
		{
			StartDate = startDate;
			EndDate = endDate;
			StartValue = startValue;
			EndValue = endValue;
			CashFlow = cashFlow;
			Activities = activities;
		}

		/// <summary>
		/// Calculate the return for this period using the Time-Weighted Return formula
		/// </summary>
		public decimal CalculatePeriodReturn()
		{
			if (StartValue.Amount == 0)
			{
				return 0;
			}

			// TWR formula: (End Value - Start Value - Cash Flow) / Start Value
			var netGain = EndValue.Amount - StartValue.Amount - CashFlow.Amount;
			return netGain / StartValue.Amount;
		}
	}
}