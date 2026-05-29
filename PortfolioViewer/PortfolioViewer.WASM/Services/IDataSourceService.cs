namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
	/// <summary>
	/// Controls whether data is fetched from the local synced database or directly from the API.
	/// </summary>
	public interface IDataSourceService
	{
		/// <summary>
		/// When <c>true</c>, data services query the API directly.
		/// When <c>false</c> (default), data services use the local synced database.
		/// </summary>
		bool UseApiDirectly { get; set; }
	}

	public class DataSourceService : IDataSourceService
	{
		public bool UseApiDirectly { get; set; }
	}
}
