using GhostfolioSidekick.Model;
using System.Collections.Generic;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Models
{
	/// <summary>
	/// Parameters for querying transactions with filtering, sorting, and pagination
	/// </summary>
	public class TransactionQueryParameters
	{
		/// <summary>
		/// Currency to convert values to
		/// </summary>
		public Currency TargetCurrency { get; set; } = Currency.USD;

		/// <summary>
		/// Start date filter
		/// </summary>
		public DateOnly StartDate { get; set; }

		/// <summary>
		/// End date filter
		/// </summary>
		public DateOnly EndDate { get; set; }

		/// <summary>
		/// Account filter (0 for all accounts)
		/// </summary>
		public int AccountId { get; set; }

		/// <summary>
		/// Symbol filter (empty for all symbols)
		/// </summary>
		public string Symbol { get; set; } = string.Empty;

		/// <summary>
		/// Transaction type filter (empty for all types)
		/// </summary>
		public List<string> TransactionTypes { get; set; } = [];

		/// <summary>
		/// Search text filter (empty for no search)
		/// </summary>
		public string SearchText { get; set; } = string.Empty;

		/// <summary>
		/// Column to sort by
		/// </summary>
		public string SortColumn { get; set; } = "Date";

		/// <summary>
		/// Sort direction
		/// </summary>
		public bool SortAscending { get; set; } = true;

		/// <summary>
		/// Page number (1-based)
		/// </summary>
		public int PageNumber { get; set; } = 1;

		/// <summary>
		/// Number of items per page
		/// </summary>
		public int PageSize { get; set; } = 25;
	}
}