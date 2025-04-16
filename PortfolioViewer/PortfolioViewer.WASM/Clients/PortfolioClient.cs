using System.Net.Http.Json;
using System.Text.Json;
using GhostfolioSidekick.Database;
using Microsoft.EntityFrameworkCore;
using static System.Runtime.InteropServices.JavaScript.JSType;

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
					const int pageSize = 1000;
					bool hasMoreData = true;

					var internalList = new List<Dictionary<string, object>>();

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

						internalList.AddRange(data);

						page++;
					} while (hasMoreData);

					currentStep++;

					// Insert the data into the database
					using (var transaction = await databaseContext.Database.BeginTransactionAsync())
					{
						progress?.Report(($"Inserting data into table: {tableName}...", (currentStep * 100) / totalSteps));

						var columns = string.Join(", ", internalList.First().Keys.Select(key => $"\"{key}\""));
						using var connection = databaseContext.Database.GetDbConnection();
						using var command = connection.CreateCommand();
						var parametersString = string.Join(", ", internalList.First().Keys.Select((key, index) => $"${key}"));
						command.CommandText =$"INSERT INTO \"{tableName}\" ({columns}) VALUES ({parametersString})";
						
						var parameters = internalList.First().Keys.Select((key, index) =>
						{
							return new Microsoft.Data.Sqlite.SqliteParameter($"${key}", 0);
						}).ToArray();
						command.Parameters.AddRange(parameters);

						foreach (var record in internalList)
						{
							foreach (var key in record.Keys)
							{
								// Set the parameter value for each record
								var parameter = parameters.FirstOrDefault(p => p.ParameterName == $"${key}");
								if (parameter != null)
								{
									object? parameterValue = record[key] switch
									{
										JsonElement jsonElement => jsonElement.ValueKind switch
										{
											JsonValueKind.String => jsonElement.GetString(),
											JsonValueKind.Number => jsonElement.TryGetInt64(out var longValue) ? longValue : jsonElement.GetDouble(),
											JsonValueKind.True => true,
											JsonValueKind.False => false,
											JsonValueKind.Null => DBNull.Value,
											JsonValueKind.Object => JsonSerializer.Serialize(record[key]),
											_ => throw new InvalidOperationException($"Unsupported JsonValueKind: {jsonElement.ValueKind} on table {tableName}. SQL was {command.CommandText}")
										},
										null => DBNull.Value,
										_ => record[key]
									};
									parameter.Value = parameterValue ?? DBNull.Value;
								}
							}
														
							await command.ExecuteNonQueryAsync(cancellationToken);
						}

						await transaction.CommitAsync(cancellationToken);

						progress?.Report(($"Data inserted successfully into table: {tableName}.", (currentStep * 100) / totalSteps));
					}
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
