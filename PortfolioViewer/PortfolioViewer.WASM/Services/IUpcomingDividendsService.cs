using System.Collections.Generic;
using System.Threading.Tasks;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
    public interface IUpcomingDividendsService
    {
        Task<List<UpcomingDividendModel>> GetUpcomingDividendsAsync();
    }
}