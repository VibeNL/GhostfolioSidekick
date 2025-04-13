using System.Net.Http.Json;
using System.Text.Json;
using GhostfolioSidekick.Database;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Clients
{
	public class PortfolioClient(HttpClient httpClient, DatabaseContext databaseContext)
	{
		private const string EfMigrationTable = "__EFMigrationsHistory";

		public async Task SyncPortfolio(CancellationToken cancellationToken = default)
		{
			try
			{
				// Check if any pending migrations
				var pendingMigrations = await databaseContext.Database.GetPendingMigrationsAsync(cancellationToken);
				if (pendingMigrations.Any())
				{
					await databaseContext.Database.MigrateAsync(cancellationToken);
				}

				// Get all tables from the database
				var tables = databaseContext.Database.SqlQueryRaw<string>("SELECT name FROM sqlite_master WHERE type='table'");
				var tableNames = await tables.ToListAsync(cancellationToken);
				if (!tableNames.Any())
				{
					return;
				}

				// Clear all tables
				foreach (var tableName in tableNames.Where(x => x != EfMigrationTable))
				{
					await databaseContext.Database.ExecuteSqlRawAsync($"DELETE FROM {tableName}", cancellationToken);
				}

				foreach (var tableName in tableNames.Where(x => x != EfMigrationTable))
				{
					int page = 1;
					const int pageSize = 100;
					bool hasMoreData = true;

					do
					{
						var response = await httpClient.GetAsync($"/api/sync/{tableName}?page={page}&pageSize={pageSize}", cancellationToken);
						if (response == null || response.StatusCode != System.Net.HttpStatusCode.OK)
						{
							break;
						}

						var rawData = await response.Content.ReadAsStringAsync(cancellationToken);
						// Check if the data is empty
						if (string.IsNullOrEmpty(rawData))
						{
							hasMoreData = false;
							break;
						}

						// Data
						var data = DeserializeData(rawData);

						// Raw insert data into the database
						foreach (var record in data)
						{
							var columns = string.Join(", ", record.Keys);
							var values = string.Join(", ", record.Values.Select(value =>
								long.TryParse(value.ToString(), out _) ? value.ToString() : $"'{value}'"));
							var sql = $"INSERT INTO {tableName} ({columns}) VALUES ({values})";
							sql = sql.Replace("{", "\\{"); // Escape special characters
							sql = sql.Replace("}", "\\}"); // Escape special characters
							sql = sql.Replace("[", "\\["); // Escape single quotes
							sql = sql.Replace("]", "\\]"); // Escape single quotes

							try
							{
								await databaseContext.Database.ExecuteSqlRawAsync(sql, cancellationToken);
							}
							catch (Exception ex)
							{
								// Handle the exception as needed
								Console.WriteLine($"Error inserting data into {tableName}: {ex.Message}");
							}
						}

						page++;
					} while (hasMoreData);
				}
			}
			catch (Exception ex)
			{
				throw;
			}
		}

		public static List<Dictionary<string, object>> DeserializeData(string jsonData)
		{
			if (string.IsNullOrEmpty(jsonData))
			{
				return new List<Dictionary<string, object>>();
			}

			return JsonSerializer.Deserialize<List<Dictionary<string, object>>>(jsonData)
				   ?? new List<Dictionary<string, object>>();
		}
	}
}
