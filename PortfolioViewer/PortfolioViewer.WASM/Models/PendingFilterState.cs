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
        public string SelectedCurrency { get; set; } = "EUR";
        public int SelectedAccountId { get; set; } = 0;
        public string SelectedSymbol { get; set; } = "";

        /// <summary>
        /// Creates a pending filter state from the current filter state
        /// </summary>
        public static PendingFilterState FromFilterState(FilterState filterState)
        {
            return new PendingFilterState
            {
                StartDate = filterState.StartDate,
                EndDate = filterState.EndDate,
                SelectedCurrency = filterState.SelectedCurrency,
                SelectedAccountId = filterState.SelectedAccountId,
                SelectedSymbol = filterState.SelectedSymbol
            };
        }

        /// <summary>
        /// Applies the pending changes to the actual filter state
        /// </summary>
        public void ApplyTo(FilterState filterState)
        {
            filterState.StartDate = StartDate;
            filterState.EndDate = EndDate;
            filterState.SelectedCurrency = SelectedCurrency;
            filterState.SelectedAccountId = SelectedAccountId;
            filterState.SelectedSymbol = SelectedSymbol;
        }

        /// <summary>
        /// Checks if the pending state is different from the current filter state
        /// </summary>
        public bool HasChanges(FilterState filterState)
        {
            return StartDate != filterState.StartDate ||
                   EndDate != filterState.EndDate ||
                   SelectedCurrency != filterState.SelectedCurrency ||
                   SelectedAccountId != filterState.SelectedAccountId ||
                   SelectedSymbol != filterState.SelectedSymbol;
        }
    }
}