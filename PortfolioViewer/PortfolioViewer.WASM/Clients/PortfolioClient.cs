using System.Net.Http.Json;
using System.Text.Json;
using GhostfolioSidekick.Database;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Clients
{
	public class PortfolioClient(HttpClient httpClient, DatabaseContext databaseContext)
	{
		private string[] TablesToIgnore = ["sqlite_sequence", "__EFMigrationsHistory", "__EFMigrationsLock"];

		public async Task SyncPortfolio(IProgress<(string action, int progress)> progress, CancellationToken cancellationToken = default)
		{
			try
			{
				// Step 1: Retrieve Table Names
				progress?.Report(("Retrieving table names...", 0));
				var tables = databaseContext.Database.SqlQueryRaw<string>("SELECT name FROM sqlite_master WHERE type='table'");
				var tableNames = await tables.ToListAsync(cancellationToken);
				if (!tableNames.Any())
				{
					progress?.Report(("No tables found in the database.", 100));
					return;
				}

				int totalSteps = tableNames.Count * 2; // Clearing + Syncing
				int currentStep = 0;

				// Step 2: Clear Tables
				foreach (var tableName in tableNames.Where(x => !TablesToIgnore.Contains(x)))
				{
					progress?.Report(($"Clearing table: {tableName}...", (currentStep * 100) / totalSteps));
					var deleteSql = $"DELETE FROM {tableName}";
					await databaseContext.Database.ExecuteSqlRawAsync(deleteSql, cancellationToken);
					currentStep++;
				}

				// Step 3: Sync Data for Each Table
				foreach (var tableName in tableNames.Where(x => !TablesToIgnore.Contains(x)))
				{
					progress?.Report(($"Syncing data for table: {tableName}...", (currentStep * 100) / totalSteps));
					int page = 1;
					const int pageSize = 100;
					bool hasMoreData = true;

					do
					{
						progress?.Report(($"Fetching page {page} for table: {tableName}...", (currentStep * 100) / totalSteps));
						var response = await httpClient.GetAsync($"/api/sync/{tableName}?page={page}&pageSize={pageSize}", cancellationToken);
						if (response == null || response.StatusCode != System.Net.HttpStatusCode.OK)
						{
							progress?.Report(($"Failed to fetch data for table: {tableName}, page: {page}.", (currentStep * 100) / totalSteps));
							break;
						}

						var rawData = await response.Content.ReadAsStringAsync(cancellationToken);
						if (string.IsNullOrEmpty(rawData))
						{
							progress?.Report(($"No more data for table: {tableName}.", (currentStep * 100) / totalSteps));
							hasMoreData = false;
							break;
						}

						var data = DeserializeData(rawData);

						if (data == null || !data.Any())
						{
							progress?.Report(($"No data found for table: {tableName}, page: {page}.", (currentStep * 100) / totalSteps));
							hasMoreData = false;
							break;
						}

						foreach (var record in data)
						{
							var columns = string.Join(", ", record.Keys.Select(key => $"\"{key}\""));
							var parameters = string.Join(", ", record.Keys.Select((key, index) => $"@p{index}"));
							var sql = $"INSERT INTO \"{tableName}\" ({columns}) VALUES ({parameters})";

							var sqlParameters = record.Values.Select((value, index) =>
							{
								object? parameterValue = value switch
								{
									JsonElement jsonElement => jsonElement.ValueKind switch
									{
										JsonValueKind.String => jsonElement.GetString(),
										JsonValueKind.Number => jsonElement.TryGetInt64(out var longValue) ? longValue : jsonElement.GetDouble(),
										JsonValueKind.True => true,
										JsonValueKind.False => false,
										JsonValueKind.Null => DBNull.Value,
										JsonValueKind.Object => JsonSerializer.Serialize(value),
										_ => throw new InvalidOperationException($"Unsupported JsonValueKind: {jsonElement.ValueKind} on table {tableName} for index {index}. SQL was {sql}")
									},
									null => DBNull.Value,
									_ => value
								};

								return new Microsoft.Data.Sqlite.SqliteParameter($"@p{index}", parameterValue ?? DBNull.Value);
							}).ToArray();

							try
							{
								await databaseContext.Database.ExecuteSqlRawAsync(sql, sqlParameters, cancellationToken);
							}
							catch (Exception ex)
							{
								progress?.Report(($"Error inserting data into {tableName}: {ex.Message}", (currentStep * 100) / totalSteps));
							}
						}

						page++;
					} while (hasMoreData);

					currentStep++;
				}

				progress?.Report(("Sync completed successfully.", 100));
			}
			catch (Exception ex)
			{
				progress?.Report(($"Error: {ex.Message}", 100));
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
