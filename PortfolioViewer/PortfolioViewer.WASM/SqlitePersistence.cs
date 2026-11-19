using GhostfolioSidekick.Database;
using Microsoft.JSInterop;

namespace GhostfolioSidekick.PortfolioViewer.WASM
{
	public class SqlitePersistence
	{
		private const string Import = "import";
		private const string JsFile = "./js/sqlite-persistence.js";

		private readonly IJSRuntime js;
		private IJSObjectReference? module;

		public SqlitePersistence(IJSRuntime js)
		{
			ArgumentNullException.ThrowIfNull(js);
			this.js = js;
		}

		public async Task InitializeDatabase()
		{
			try
			{
				module ??= await js.InvokeAsync<IJSObjectReference>(Import, JsFile);

				Console.WriteLine("JavaScript module loaded");

				await module.InvokeVoidAsync("setupDatabase", DatabaseContext.DbFileName);
				Console.WriteLine("Database setup completed");
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				Console.WriteLine($"Failed to initialize database: {ex.Message}");
				throw;
			}
		}

		public async Task SaveChangesAsync()
		{
			try
			{
				module ??= await js.InvokeAsync<IJSObjectReference>(Import, JsFile);

				Console.WriteLine("Syncing database to IndexedDB");

				await module.InvokeVoidAsync("syncDatabaseToIndexedDb", DatabaseContext.DbFileName);
				Console.WriteLine("Database sync completed");
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				Console.WriteLine($"Failed to sync database: {ex.Message}");
				throw;
			}
		}

		public async Task ClearDatabaseFromIndexedDb()
		{
			try
			{
				module ??= await js.InvokeAsync<IJSObjectReference>(Import, JsFile);

				Console.WriteLine("Clearing database from IndexedDB");

				await module.InvokeVoidAsync("clearDatabaseFromIndexedDb");
				Console.WriteLine("Database cleared from IndexedDB");
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				Console.WriteLine($"Failed to clear database from IndexedDB: {ex.Message}");
				throw;
			}
		}

		public async Task DebugFileSystem()
		{
			try
			{
				module ??= await js.InvokeAsync<IJSObjectReference>(Import, JsFile);

				await module.InvokeVoidAsync("debugFileSystem", DatabaseContext.DbFileName);
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				Console.WriteLine($"Failed to debug file system: {ex.Message}");
				throw;
			}
		}
	}
}
