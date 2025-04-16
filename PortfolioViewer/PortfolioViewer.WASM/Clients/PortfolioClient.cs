using System.Net.Http.Json;
using System.Runtime.CompilerServices;
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
				var tables = databaseContext.SqlQueryRaw<string>("SELECT name FROM sqlite_master WHERE type='table'");
				var tableNames = await tables.ToListAsync(cancellationToken);
				if (!tableNames.Any())
				{
					progress?.Report(("No tables found in the database.", 100));
					return;
				}

				int totalSteps = tableNames.Count; // Clearing + Syncing
				int currentStep = 0;

				// Step 2: Clear Tables
				foreach (var tableName in tableNames.Where(x => !TablesToIgnore.Contains(x)))
				{
					progress?.Report(($"Clearing table: {tableName}...", 0));
					var deleteSql = $"DELETE FROM {tableName}";
					await databaseContext.ExecuteSqlRawAsync(deleteSql, cancellationToken);
				}

				// Disable contraints on DB
				await databaseContext.ExecutePragma("PRAGMA foreign_keys=OFF;");
				await databaseContext.ExecutePragma("PRAGMA synchronous=OFF;");
				await databaseContext.ExecutePragma("PRAGMA journal_mode=MEMORY;");
				await databaseContext.ExecutePragma("PRAGMA cache_size =1000000;");
				await databaseContext.ExecutePragma("PRAGMA locking_mode=EXCLUSIVE;");
				await databaseContext.ExecutePragma("PRAGMA temp_store =MEMORY;");
				await databaseContext.ExecutePragma("PRAGMA auto_vacuum=0;");


				// Step 3: Sync Data for Each Table
				foreach (var tableName in tableNames.Where(x => !TablesToIgnore.Contains(x)))
				{
					var totalWritten = 0;
					progress?.Report(($"Syncing data for table: {tableName}...", (currentStep * 100) / totalSteps));
					var semaphore = new SemaphoreSlim(1); // Limit to 1 concurrent task since Webassembly is single threaded
					var tasks = new List<Task>();

					await foreach (var dataChunk in FetchDataAsync(tableName, cancellationToken))
					{
						await semaphore.WaitAsync(cancellationToken);
						tasks.Add(Task.Run(async () =>
						{
							try
							{
								progress?.Report(($"Inserting data into table: {tableName}...", (currentStep * 100) / totalSteps));
								await InsertDataAsync(tableName, dataChunk, cancellationToken);
								Interlocked.Add(ref totalWritten, dataChunk.Count);
								progress?.Report(($"Inserted total written {totalWritten} into table: {tableName}...", (currentStep * 100) / totalSteps));
							}
							finally
							{
								semaphore.Release();
							}
						}, cancellationToken));
					}

					await Task.WhenAll(tasks);
					currentStep++;
				}

				// Step 4: Enable constraints on DB
				await databaseContext.ExecutePragma("PRAGMA foreign_keys=ON;");
				await databaseContext.ExecutePragma("PRAGMA synchronous=FULL;");
				await databaseContext.ExecutePragma("PRAGMA journal_mode=DELETE;");
				await databaseContext.ExecutePragma("PRAGMA auto_vacuum=FULL;");


				progress?.Report(("Sync completed successfully.", 100));
			}
			catch (Exception ex)
			{
				progress?.Report(($"Error: {ex.Message}", 100));
				throw;
			}
		}

		private async IAsyncEnumerable<List<Dictionary<string, object>>> FetchDataAsync(string tableName, [EnumeratorCancellation] CancellationToken cancellationToken)
		{
			int page = 1;
			const int pageSize = 1000;
			bool hasMoreData = true;

			while (hasMoreData)
			{
				var response = await httpClient.GetAsync($"/api/sync/{tableName}?page={page}&pageSize={pageSize}", cancellationToken);
				if (response == null || response.StatusCode != System.Net.HttpStatusCode.OK)
				{
					yield break;
				}

				var rawData = await response.Content.ReadAsStringAsync(cancellationToken);
				if (string.IsNullOrEmpty(rawData))
				{
					yield break;
				}

				var data = DeserializeData(rawData);
				if (data == null || !data.Any())
				{
					yield break;
				}

				yield return data;
				page++;
			}
		}

		private async Task InsertDataAsync(string tableName, List<Dictionary<string, object>> dataChunk, CancellationToken cancellationToken)
		{
			using var transaction = await databaseContext.Database.BeginTransactionAsync(cancellationToken);
			var columns = string.Join(", ", dataChunk.First().Keys.Select(key => $"\"{key}\""));
			using var connection = databaseContext.Database.GetDbConnection();
			using var command = connection.CreateCommand();
			var parametersString = string.Join(", ", dataChunk.First().Keys.Select((key, index) => $"${key}"));
			command.CommandText = $"INSERT INTO \"{tableName}\" ({columns}) VALUES ({parametersString})";

			var parameters = dataChunk.First().Keys.Select((key, index) =>
			{
				return new Microsoft.Data.Sqlite.SqliteParameter($"${key}", 0);
			}).ToArray();
			command.Parameters.AddRange(parameters);

			foreach (var record in dataChunk)
			{
				foreach (var key in record.Keys)
				{
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
		}

		private static readonly JsonSerializerOptions JsonOptions = new()
		{
			PropertyNameCaseInsensitive = false, // Avoid case-insensitive matching
			DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
			WriteIndented = false, // Avoid unnecessary formatting
			AllowTrailingCommas = true
		};

		public static List<Dictionary<string, object>> DeserializeData(string jsonData)
		{
			if (string.IsNullOrWhiteSpace(jsonData))
			{
				return new List<Dictionary<string, object>>();
			}

			try
			{
				// Use JsonDocument for efficient parsing
				using var document = JsonDocument.Parse(jsonData);
				var result = new List<Dictionary<string, object>>();

				foreach (var element in document.RootElement.EnumerateArray())
				{
					var dictionary = new Dictionary<string, object>();
					foreach (var property in element.EnumerateObject())
					{
						dictionary[property.Name] = property.Value.ValueKind switch
						{
							JsonValueKind.String => property.Value.GetString(),
							JsonValueKind.Number => property.Value.TryGetInt64(out var longValue) ? longValue : property.Value.GetDouble(),
							JsonValueKind.True => true,
							JsonValueKind.False => false,
							JsonValueKind.Null => null!,
							JsonValueKind.Object => property.Value.GetRawText(), // Serialize nested objects as JSON
							_ => property.Value.GetRawText() // Fallback for arrays or unsupported types
						};
					}
					result.Add(dictionary);
				}

				return result;
			}
			catch (JsonException ex)
			{
				// Log or handle JSON parsing errors if necessary
				throw new InvalidOperationException("Failed to deserialize JSON data.", ex);
			}
		}
	}
}
