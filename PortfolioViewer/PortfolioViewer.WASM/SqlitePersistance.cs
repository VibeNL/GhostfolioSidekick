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

		internal async Task SaveChangesAsync(CancellationToken cancellationToken)
		{
			if (module == null)
			{
				module = await js.InvokeAsync<IJSObjectReference>("import", "./js/sqlite-persistence.js");
			}

			Console.WriteLine("JavaScript module loaded");

			await module.InvokeVoidAsync("syncDatabaseToIndexedDb", DatabaseContext.DbFileName);
		}
	}
}
