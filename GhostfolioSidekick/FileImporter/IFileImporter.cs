using GhostfolioSidekick.Ghostfolio.API;

namespace GhostfolioSidekick.FileImporter
{
    public interface IFileImporter
    {
        Task<bool> CanConvertOrders(string file);
        Task<IEnumerable<Order>> ConvertToOrders(string accountName, string filename);
    }
}
