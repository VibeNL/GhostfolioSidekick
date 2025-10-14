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
			module ??= await js.InvokeAsync<IJSObjectReference>(Import, JsFile);

			Console.WriteLine("JavaScript module loaded");

			await module.InvokeVoidAsync("setupDatabase", DatabaseContext.DbFileName);
			Console.WriteLine("Database setup completed");
		}

		public async Task SaveChangesAsync()
		{
			module ??= await js.InvokeAsync<IJSObjectReference>(Import, JsFile);

			Console.WriteLine("Syncing database to IndexedDB");

			await module.InvokeVoidAsync("syncDatabaseToIndexedDb", DatabaseContext.DbFileName);
			Console.WriteLine("Database sync completed");
		}

		public async Task ClearDatabaseFromIndexedDb()
		{
			module ??= await js.InvokeAsync<IJSObjectReference>(Import, JsFile);

			Console.WriteLine("Clearing database from IndexedDB");

			await module.InvokeVoidAsync("clearDatabaseFromIndexedDb");
			Console.WriteLine("Database cleared from IndexedDB");
		}

		public async Task DebugFileSystem()
		{
			module ??= await js.InvokeAsync<IJSObjectReference>(Import, JsFile);

			await module.InvokeVoidAsync("debugFileSystem", DatabaseContext.DbFileName);
		}
	}
}
