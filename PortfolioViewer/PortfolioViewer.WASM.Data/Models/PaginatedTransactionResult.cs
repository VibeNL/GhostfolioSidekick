namespace GhostfolioSidekick.PortfolioViewer.WASM.Models
{
    public class PaginatedTransactionResult
    {
        public List<TransactionDisplayModel> Transactions { get; set; } = new();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public bool HasNextPage => PageNumber < TotalPages;
        public bool HasPreviousPage => PageNumber > 1;
        
        /// <summary>
        /// Summary statistics for all filtered transactions (not just current page)
        /// </summary>
        public Dictionary<string, int> TransactionTypeBreakdown { get; set; } = new();
        public Dictionary<string, int> AccountBreakdown { get; set; } = new();
    }
}