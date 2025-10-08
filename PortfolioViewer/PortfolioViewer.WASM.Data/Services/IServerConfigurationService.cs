using GhostfolioSidekick.Model;
using System.Threading.Tasks;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services
{
	public interface IServerConfigurationService
	{
		/// <summary>
		/// Gets the primary currency. If not loaded yet, returns EUR as default.
		/// Use GetPrimaryCurrencyAsync() to ensure the currency is loaded from the server.
		/// </summary>
		Currency PrimaryCurrency { get; }
		
		/// <summary>
		/// Loads and returns the primary currency from the server.
		/// </summary>
		Task<Currency> GetPrimaryCurrencyAsync();
	}
}
