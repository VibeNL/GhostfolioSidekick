using System.Runtime.CompilerServices;
using System.Text.Json;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.PortfolioViewer.Model;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Clients
{
	public class PortfolioClient(HttpClient httpClient, DatabaseContext databaseContext)
	{
		private string[] TablesToIgnore = ["sqlite_sequence", "__EFMigrationsHistory", "__EFMigrationsLock"];

		const int pageSize = 10000;

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
				foreach (var tableName in tableNames.Where(x => !TablesToIgnore.Contains(x)).OrderBy(x => x))
				{
					var totalWritten = 0;
					progress?.Report(($"Syncing data for table: {tableName}...", (currentStep * 100) / totalSteps));

					await foreach (var dataChunk in FetchDataAsync(tableName, cancellationToken))
					{
						progress?.Report(($"Inserting data into table: {tableName}...", (currentStep * 100) / totalSteps));
						await InsertDataAsync(tableName, dataChunk, cancellationToken);
						totalWritten += dataChunk.Count;
						progress?.Report(($"Inserted total written {totalWritten} into table: {tableName}...", (currentStep * 100) / totalSteps));
					}
								
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
			Console.WriteLine($"InsertDataAsync executing");

			var stopwatch = System.Diagnostics.Stopwatch.StartNew(); // Start timing

			using var transaction = await databaseContext.Database.BeginTransactionAsync(cancellationToken);
			var columns = string.Join(", ", dataChunk.First().Keys.Select(key => $"\"{key}\""));
			using var connection = databaseContext.Database.GetDbConnection();
			using var command = connection.CreateCommand();
			var parametersString = string.Join(", ", dataChunk.First().Keys.Select((key, index) => $"${key}"));
			command.CommandText = $"INSERT INTO \"{tableName}\" ({columns}) VALUES ({parametersString})";

			var parameters = dataChunk.First().Keys.Select((key, index) =>
			{
				return new Microsoft.Data.Sqlite.SqliteParameter($"${key}", 0);
			}).ToDictionary(x => x.ParameterName.Trim('$'), x => x);
			command.Parameters.AddRange(parameters.Values.ToArray());

			foreach (var record in dataChunk)
			{
				foreach (var key in record.Keys)
				{
					var parameter = parameters[key];
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

			stopwatch.Stop(); // Stop timing
			Console.WriteLine($"InsertDataAsync executed in {stopwatch.ElapsedMilliseconds} ms");
		}


		public static List<Dictionary<string, object>> DeserializeData(string jsonData)
		{
			Console.WriteLine($"DeserializeData executing");
			var stopwatch = System.Diagnostics.Stopwatch.StartNew(); // Start timing

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

				stopwatch.Stop(); // Stop timing
				Console.WriteLine($"DeserializeData executed in {stopwatch.ElapsedMilliseconds} ms");

				return result;
			}
			catch (JsonException ex)
			{
				// Log or handle JSON parsing errors if necessary
				throw new InvalidOperationException("Failed to deserialize JSON data.", ex);
			}
		}

		public async Task<List<MarketData>> GetValueOverTimeData()
		{
			var response = await httpClient.GetAsync("/api/overview/valueovertime");
			response.EnsureSuccessStatusCode();
			var jsonData = await response.Content.ReadAsStringAsync();
			return JsonSerializer.Deserialize<List<MarketData>>(jsonData) ?? new List<MarketData>();
		}

		public async Task<List<MarketData>> GetProfitOverTimeData()
		{
			var response = await httpClient.GetAsync("/api/overview/profitovertime");
			response.EnsureSuccessStatusCode();
			var jsonData = await response.Content.ReadAsStringAsync();
			return JsonSerializer.Deserialize<List<MarketData>>(jsonData) ?? new List<MarketData>();
		}

		public async Task<List<MarketData>> GetDividendsPerMonthData()
		{
			var response = await httpClient.GetAsync("/api/overview/dividendspermonth");
			response.EnsureSuccessStatusCode();
			var jsonData = await response.Content.ReadAsStringAsync();
			return JsonSerializer.Deserialize<List<MarketData>>(jsonData) ?? new List<MarketData>();
		}
	}
}
