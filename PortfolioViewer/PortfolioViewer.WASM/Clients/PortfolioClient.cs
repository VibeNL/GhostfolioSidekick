using System.Runtime.CompilerServices;
using System.Text.Json;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.PortfolioViewer.ApiService.Grpc;
using Microsoft.EntityFrameworkCore;
using Grpc.Net.Client.Web;
using Grpc.Net.Client;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Clients
{
	public class PortfolioClient(HttpClient httpClient, DatabaseContext databaseContext, ILogger<PortfolioClient> logger) : IDisposable
	{
		private string[] TablesToIgnore = ["sqlite_sequence", "__EFMigrationsHistory", "__EFMigrationsLock"]; // TODO 

		const int pageSize = 10000;

		private GrpcChannel? _grpcChannel;
		private SyncService.SyncServiceClient? _grpcClient;
		private bool _disposed = false;

		private SyncService.SyncServiceClient GetGrpcClient()
		{
			if (_grpcClient != null)
				return _grpcClient;

			// Create gRPC channel for web - use the httpClient's base address but ensure it's absolute
			var baseAddress = httpClient.BaseAddress;
			if (baseAddress == null)
			{
				throw new InvalidOperationException("HttpClient BaseAddress is not configured.");
			}

			// For Blazor WASM, we need to use the actual HTTP URL, not the service discovery name
			var grpcAddress = baseAddress.ToString().TrimEnd('/');
			
			logger.LogInformation("Creating gRPC channel for address: {GrpcAddress}", grpcAddress);

			_grpcChannel = GrpcChannel.ForAddress(grpcAddress, new GrpcChannelOptions
			{
				HttpHandler = new GrpcWebHandler(GrpcWebMode.GrpcWeb, new HttpClientHandler()),
				MaxReceiveMessageSize = 16 * 1024 * 1024, // 16MB
				MaxSendMessageSize = 16 * 1024 * 1024, // 16MB
				ThrowOperationCanceledOnCancellation = true
			});

			_grpcClient = new SyncService.SyncServiceClient(_grpcChannel);
			return _grpcClient;
		}

		public async Task SyncPortfolio(IProgress<(string action, int progress)> progress, CancellationToken cancellationToken = default)
		{
			try
			{
				var grpcClient = GetGrpcClient();

				// Step 1: Retrieve Table Names
				progress?.Report(("Retrieving table names...", 0));
				var tableNamesResponse = await grpcClient.GetTableNamesAsync(new GetTableNamesRequest(), cancellationToken: cancellationToken);
				var tableNames = tableNamesResponse.TableNames.ToList();
				
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

					await foreach (var dataChunk in FetchDataAsync(grpcClient, tableName, cancellationToken))
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
				logger.LogError(ex, "Error during portfolio sync: {Message}", ex.Message);
				progress?.Report(($"Error: {ex.Message}", 100));
				throw;
			}
		}

		private async IAsyncEnumerable<List<Dictionary<string, object>>> FetchDataAsync(SyncService.SyncServiceClient grpcClient, string tableName, [EnumeratorCancellation] CancellationToken cancellationToken)
		{
			int page = 1;
			bool hasMoreData = true;

			while (hasMoreData)
			{
				var request = new GetEntityDataRequest
				{
					Entity = tableName,
					Page = page,
					PageSize = pageSize
				};

				using var call = grpcClient.GetEntityData(request, cancellationToken: cancellationToken);

				while (await call.ResponseStream.MoveNext(cancellationToken))
				{
					var response = call.ResponseStream.Current;
					
					if (response.Records.Count == 0)
					{
						hasMoreData = false;
						yield break;
					}

					// Convert gRPC response to the expected format
					var data = response.Records.Select(record =>
					{
						var dictionary = new Dictionary<string, object>();
						foreach (var field in record.Fields)
						{
							// Convert string back to appropriate type
							dictionary[field.Key] = ConvertStringToValue(field.Value);
						}
						return dictionary;
					}).ToList();

					yield return data;

					hasMoreData = response.HasMore;
					page++;

					if (!hasMoreData)
						break;
				}

				if (!hasMoreData)
					break;
			}
		}

		private static object ConvertStringToValue(string value)
		{
			if (string.IsNullOrEmpty(value))
				return DBNull.Value;

			// Try to parse as different types
			if (long.TryParse(value, out var longValue))
				return longValue;
			
			if (double.TryParse(value, out var doubleValue))
				return doubleValue;
			
			if (bool.TryParse(value, out var boolValue))
				return boolValue;
			
			// Try to parse as JSON for complex objects
			if (value.StartsWith("{") || value.StartsWith("["))
			{
				try
				{
					return JsonDocument.Parse(value).RootElement;
				}
				catch
				{
					// If it fails, just return as string
				}
			}

			return value;
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
							JsonValueKind.String => property.Value.GetString() ?? string.Empty,
							JsonValueKind.Number => property.Value.TryGetInt64(out var longValue) ? longValue : property.Value.GetDouble(),
							JsonValueKind.True => true,
							JsonValueKind.False => false,
							JsonValueKind.Null => DBNull.Value,
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

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected virtual void Dispose(bool disposing)
		{
			if (!_disposed && disposing)
			{
				_grpcChannel?.Dispose();
				_disposed = true;
			}
		}
	}
}
