namespace GhostfolioSidekick.PortfolioViewer.WASM.Models
{
    /// <summary>
    /// Holds pending filter changes that haven't been applied yet.
    /// This allows users to make multiple filter changes before applying them all at once.
    /// </summary>
    public class PendingFilterState
    {
        public DateOnly StartDate { get; set; }
        public DateOnly EndDate { get; set; }
		public int SelectedAccountId { get; set; }
		public string SelectedSymbol { get; set; } = "";
        public string SelectedTransactionType { get; set; } = "";
        public string SearchText { get; set; } = "";

        /// <summary>
        /// Creates a pending filter state from the current filter state
        /// </summary>
        public static PendingFilterState FromFilterState(FilterState filterState)
        {
            return new PendingFilterState
            {
                StartDate = filterState.StartDate,
                EndDate = filterState.EndDate,
                SelectedAccountId = filterState.SelectedAccountId,
                SelectedSymbol = filterState.SelectedSymbol,
                SelectedTransactionType = filterState.SelectedTransactionType,
                SearchText = filterState.SearchText
            };
        }

        /// <summary>
        /// Applies the pending changes to the actual filter state
        /// </summary>
        public void ApplyTo(FilterState filterState)
        {
            filterState.StartDate = StartDate;
            filterState.EndDate = EndDate;
            filterState.SelectedAccountId = SelectedAccountId;
            filterState.SelectedSymbol = SelectedSymbol;
            filterState.SelectedTransactionType = SelectedTransactionType;
            filterState.SearchText = SearchText;
        }

        /// <summary>
        /// Checks if the pending state is different from the current filter state
        /// </summary>
        public bool HasChanges(FilterState filterState)
        {
            return StartDate != filterState.StartDate ||
                   EndDate != filterState.EndDate ||
                   SelectedAccountId != filterState.SelectedAccountId ||
                   SelectedSymbol != filterState.SelectedSymbol ||
                   SelectedTransactionType != filterState.SelectedTransactionType ||
                   SearchText != filterState.SearchText;
        }
    }
}