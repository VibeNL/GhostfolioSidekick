namespace GhostfolioSidekick.Ghostfolio.API
{
    public interface IGhostfolioAPI
    {
        Task<Asset> FindSymbolByISIN(string? isin);
        
        Task<decimal> GetExchangeRate(string sourceCurrency, string targetCurrency, DateTime date);
        
        Task<decimal> GetMarketPrice(string symbol, DateTime date);

        Task<Account> GetAccountByName(string name);

        public Task Write(IEnumerable<Order> orders);
    }
}
