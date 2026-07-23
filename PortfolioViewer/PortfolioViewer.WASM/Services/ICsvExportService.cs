namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
	/// <summary>
	/// Service for exporting data to CSV and triggering browser download.
	/// </summary>
	public interface ICsvExportService
	{
		/// <summary>
		/// Exports a list of objects to CSV and triggers a browser download.
		/// </summary>
		/// <typeparam name="T">The type of objects to export.</typeparam>
		/// <param name="data">The data to export.</param>
		/// <param name="fileName">The name of the file to download (without .csv extension).</param>
		/// <param name="headers">Optional custom headers. If null, property names are used.</param>
		Task ExportToCsvAsync<T>(IEnumerable<T> data, string fileName, IEnumerable<string>? headers = null);

		/// <summary>
		/// Exports a list of objects to a CSV string.
		/// </summary>
		/// <typeparam name="T">The type of objects to export.</typeparam>
		/// <param name="data">The data to export.</param>
		/// <param name="headers">Optional custom headers. If null, property names are used.</param>
		string ExportToCsvString<T>(IEnumerable<T> data, IEnumerable<string>? headers = null);
	}
}
