using GhostfolioSidekick.PortfolioViewer.Services.Models;

namespace GhostfolioSidekick.PortfolioViewer.Services.Interfaces;

public interface IPortfolioValueService
{
    Task<List<string>> GetAvailableCurrenciesAsync();
    Task<List<PortfolioValuePoint>> GetPortfolioValueOverTimeAsync(string timeframe, string currency);
    Task<PortfolioSummary> GetPortfolioSummaryAsync(List<PortfolioValuePoint> portfolioData, string currency);
    Task<List<AccountBreakdown>> GetPortfolioBreakdownAsync(string currency);
}