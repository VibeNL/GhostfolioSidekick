namespace GhostfolioSidekick.Model.Portfolio
{
	/// <summary>
	/// Represents portfolio performance metrics including Time-Weighted Return, dividend analysis, and currency impact
	/// </summary>
	public record PortfolioPerformance
	{
		/// <summary>
		/// Time-Weighted Return as a percentage
		/// </summary>
		public decimal TimeWeightedReturn { get; init; }

		/// <summary>
		/// Total dividend amount in base currency
		/// </summary>
		public Money TotalDividends { get; init; }

		/// <summary>
		/// Dividend yield as a percentage (dividends / average portfolio value)
		/// </summary>
		public decimal DividendYield { get; init; }

		/// <summary>
		/// Currency impact on returns as a percentage
		/// </summary>
		public decimal CurrencyImpact { get; init; }

		/// <summary>
		/// Start date of the performance calculation period
		/// </summary>
		public DateTime StartDate { get; init; }

		/// <summary>
		/// End date of the performance calculation period
		/// </summary>
		public DateTime EndDate { get; init; }

		/// <summary>
		/// Base currency for all calculations
		/// </summary>
		public Currency BaseCurrency { get; init; }

		/// <summary>
		/// Initial portfolio value at start date
		/// </summary>
		public Money InitialValue { get; init; }

		/// <summary>
		/// Final portfolio value at end date
		/// </summary>
		public Money FinalValue { get; init; }

		/// <summary>
		/// Total cash flows (deposits minus withdrawals) during the period
		/// </summary>
		public Money NetCashFlows { get; init; }

		public PortfolioPerformance()
		{
			// EF Core
			TotalDividends = new Money();
			BaseCurrency = Currency.USD;
			InitialValue = new Money();
			FinalValue = new Money();
			NetCashFlows = new Money();
		}

		public PortfolioPerformance(
			decimal timeWeightedReturn,
			Money totalDividends,
			decimal dividendYield,
			decimal currencyImpact,
			DateTime startDate,
			DateTime endDate,
			Currency baseCurrency,
			Money initialValue,
			Money finalValue,
			Money netCashFlows)
		{
			TimeWeightedReturn = timeWeightedReturn;
			TotalDividends = totalDividends;
			DividendYield = dividendYield;
			CurrencyImpact = currencyImpact;
			StartDate = startDate;
			EndDate = endDate;
			BaseCurrency = baseCurrency;
			InitialValue = initialValue;
			FinalValue = finalValue;
			NetCashFlows = netCashFlows;
		}
	}
}