using GhostfolioSidekick.Database;
using Microsoft.JSInterop;

namespace GhostfolioSidekick.PortfolioViewer.WASM
{
	public class SqlitePersistance
	{
		private readonly IJSRuntime js;
		private IJSObjectReference module;

		public SqlitePersistance(IJSRuntime js)
		{
			ArgumentNullException.ThrowIfNull(js);
			this.js = js;
		}

		public async Task InitializeDatabase()
		{
			if (module == null)
			{
				module = await js.InvokeAsync<IJSObjectReference>("import", "./js/sqlite-persistence.js");
			}

			Console.WriteLine("JavaScript module loaded");

			await module.InvokeVoidAsync("setupDatabase", DatabaseContext.DbFileName);
			Console.WriteLine("Database setup completed");
		}

		public async Task SaveChangesAsync(CancellationToken cancellationToken = default)
		{
			if (module == null)
			{
				module = await js.InvokeAsync<IJSObjectReference>("import", "./js/sqlite-persistence.js");
			}

			Console.WriteLine("Syncing database to IndexedDB");

			await module.InvokeVoidAsync("syncDatabaseToIndexedDb", DatabaseContext.DbFileName);
			Console.WriteLine("Database sync completed");
		}

		public async Task DebugFileSystem()
		{
			if (module == null)
			{
				module = await js.InvokeAsync<IJSObjectReference>("import", "./js/sqlite-persistence.js");
			}

			await module.InvokeVoidAsync("debugFileSystem", DatabaseContext.DbFileName);
		}
	}
}
