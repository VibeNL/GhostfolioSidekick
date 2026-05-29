namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
	/// <summary>
	/// Keys used to distinguish keyed DI registrations for local-DB versus API-backed data services.
	/// </summary>
	public static class DataSourceKeys
	{
		public const string Local = "local";
		public const string Api = "api";
	}
}
