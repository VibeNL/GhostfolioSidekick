using GhostfolioSidekick.Database;
using GhostfolioSidekick.PortfolioViewer.ApiService.Grpc;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Clients
{
	public class PortfolioClient(HttpClient httpClient, SqlitePersistence sqlitePersistence, IDbContextFactory<DatabaseContext> dbContextFactory, ILogger<PortfolioClient> logger) : IDisposable
	{
		private string[] TablesToIgnore = ["sqlite_sequence", "__EFMigrationsHistory", "__EFMigrationsLock"]; 

		const int pageSize = 10_000;

		private GrpcChannel? _grpcChannel;
		private SyncService.SyncServiceClient? _grpcClient;
		private bool _disposed = false;

		public async Task SyncPortfolio(IProgress<(string action, int progress)> progress, CancellationToken cancellationToken = default)
		{
			try
			{
				// Step 0: Ensure Database is Up-to-Date
				await using var databaseContext = await dbContextFactory.CreateDbContextAsync();
				var pendingMigrations = await databaseContext.Database.GetPendingMigrationsAsync();
				if (pendingMigrations.Any())
				{
					await databaseContext.Database.MigrateAsync();
				}

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
				// Disable contraints on DB
				await databaseContext.ExecutePragma("PRAGMA foreign_keys=OFF;");
				await databaseContext.ExecutePragma("PRAGMA synchronous=OFF;");
				await databaseContext.ExecutePragma("PRAGMA journal_mode=MEMORY;");
				await databaseContext.ExecutePragma("PRAGMA cache_size =1000000;");
				await databaseContext.ExecutePragma("PRAGMA locking_mode=EXCLUSIVE;");
				await databaseContext.ExecutePragma("PRAGMA temp_store =MEMORY;");
				await databaseContext.ExecutePragma("PRAGMA auto_vacuum=0;");

				foreach (var tableName in tableNames.Where(x => !TablesToIgnore.Contains(x)))
				{
					progress?.Report(($"Clearing table: {tableName}...", 0));
					var deleteSql = $"DELETE FROM {tableName}";
					await databaseContext.ExecuteSqlRawAsync(deleteSql, cancellationToken);
				}
				
				// Step 3: Sync Data for Each Table
				foreach (var tableName in tableNames.Where(x => !TablesToIgnore.Contains(x)).OrderBy(x => x))
				{
					var totalWritten = 0;
					progress?.Report(($"Syncing data for table: {tableName}...", (currentStep * 100) / totalSteps));

					await foreach (var dataChunk in FetchDataAsync(grpcClient, tableName, cancellationToken))
					{
						progress?.Report(($"Inserting data into table: {tableName}...", (currentStep * 100) / totalSteps));
						await InsertDataAsync(databaseContext, tableName, dataChunk, cancellationToken);
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

				// Step 5: Finalize sync
				await databaseContext.ExecutePragma("PRAGMA journal_mode = DELETE;"); // Use simpler journaling mode
				await databaseContext.ExecutePragma("PRAGMA synchronous = FULL;"); // Force immediate writes
				await databaseContext.ExecutePragma("PRAGMA cache_size = -2000;"); // Limit cache size to force writes

				await sqlitePersistence.SaveChangesAsync(cancellationToken);

				progress?.Report(("Sync completed successfully.", 100));

			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error during portfolio sync: {Message}", ex.Message);
				progress?.Report(($"Error: {ex.Message}", 100));
				throw;
			}
		}

		private SyncService.SyncServiceClient GetGrpcClient()
		{
			if (_grpcClient != null)
			{
				return _grpcClient;
			}

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
				MaxReceiveMessageSize = 100 * 1024 * 1024, // 100MB
				MaxSendMessageSize = 100 * 1024 * 1024, // 100MB
				ThrowOperationCanceledOnCancellation = true
			});

			_grpcClient = new SyncService.SyncServiceClient(_grpcChannel);
			return _grpcClient;
		}

		private async IAsyncEnumerable<List<Dictionary<string, object>>> FetchDataAsync(SyncService.SyncServiceClient grpcClient, string tableName, [EnumeratorCancellation] CancellationToken cancellationToken)
		{
			int page = 1;
			bool hasMore = true;

			logger.LogInformation("Starting to fetch data for table: {TableName}", tableName);

			while (hasMore)
			{
				logger.LogDebug("Fetching page {Page} for table {TableName} with page size {PageSize}", page, tableName, pageSize);

				var request = new GetEntityDataRequest
				{
					Entity = tableName,
					Page = page,
					PageSize = pageSize
				};

				using var call = grpcClient.GetEntityData(request, cancellationToken: cancellationToken);

				bool receivedData = false;
				while (await call.ResponseStream.MoveNext(cancellationToken))
				{
					var response = call.ResponseStream.Current;
					receivedData = true;

					logger.LogDebug("Received {RecordCount} records for page {Page} of table {TableName}, HasMore: {HasMore}",
						response.Records.Count, page, tableName, response.HasMore);

					if (response.Records.Count == 0)
					{
						yield break;
					}

					// Pre-allocate the list with known capacity for better performance
					var data = new List<Dictionary<string, object>>(response.Records.Count);

					// Convert gRPC response to the expected format with optimized loop
					foreach (var record in response.Records)
					{
						var dictionary = new Dictionary<string, object>(record.Fields.Count);
						foreach (var field in record.Fields)
						{
							// Convert string back to appropriate type
							dictionary[field.Key] = ConvertStringToValue(field.Value);
						}

						data.Add(dictionary);
					}

					yield return data;

					// Check if there's more data based on the response
					hasMore = response.HasMore;
				}

				if (!receivedData)
				{
					logger.LogDebug("No data received for page {Page} of table {TableName}", page, tableName);
					hasMore = false;
				}

				page++;
			}

			logger.LogInformation("Completed fetching data for table: {TableName}, total pages: {TotalPages}", tableName, page - 1);
		}

		private static readonly char[] _jsonStartChars = ['{', '['];

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static object ConvertStringToValue(string value)
		{
			// Handle null or empty strings - convert to appropriate type based on context
			if (string.IsNullOrEmpty(value))
				return string.Empty; // Convert null/empty to empty string instead of DBNull.Value

			// Fast path for common string values that don't need parsing
			if (value.Length > 0 && !char.IsDigit(value[0]) && value[0] != '-' && value[0] != '+' &&
				!_jsonStartChars.Contains(value[0]) && !value.Equals("true", StringComparison.OrdinalIgnoreCase) &&
				!value.Equals("false", StringComparison.OrdinalIgnoreCase))
			{
				return value;
			}

			// Fast boolean check
			if (value.Length <= 5)
			{
				if (value.Equals("true", StringComparison.OrdinalIgnoreCase))
					return true;
				if (value.Equals("false", StringComparison.OrdinalIgnoreCase))
					return false;
			}

			// Try numeric parsing with culture-invariant approach for better performance
			ReadOnlySpan<char> span = value.AsSpan();

			// Try long first (most common integer type)
			if (long.TryParse(span, NumberStyles.Integer, CultureInfo.InvariantCulture, out var longValue))
				return longValue;

			// Try double for decimal values
			if (double.TryParse(span, NumberStyles.Float, CultureInfo.InvariantCulture, out var doubleValue))
				return doubleValue;

			// Only try JSON parsing if it starts with { or [
			if (value.Length > 1 && _jsonStartChars.Contains(value[0]))
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static object? ConvertValueForParameter(object? value)
		{
			return value switch
			{
				JsonElement jsonElement => jsonElement.ValueKind switch
				{
					JsonValueKind.String => jsonElement.GetString(),
					JsonValueKind.Number => jsonElement.TryGetInt64(out var longValue) ? longValue : jsonElement.GetDouble(),
					JsonValueKind.True => true,
					JsonValueKind.False => false,
					JsonValueKind.Null => null,
					JsonValueKind.Object => JsonSerializer.Serialize(jsonElement),
					JsonValueKind.Array => JsonSerializer.Serialize(jsonElement),
					_ => throw new InvalidOperationException($"Unsupported JsonValueKind: {jsonElement.ValueKind}")
				},
				null => null,
				_ => value
			};
		}

		private async Task InsertDataAsync(DatabaseContext databaseContext, string tableName, List<Dictionary<string, object>> dataChunk, CancellationToken cancellationToken)
		{
			Console.WriteLine($"InsertDataAsync executing");

			var stopwatch = System.Diagnostics.Stopwatch.StartNew(); // Start timing

			// Early return if no data to insert
			if (dataChunk.Count == 0)
			{
				logger.LogInformation("No records to insert for table {TableName}", tableName);
				return;
			}

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
						object? parameterValue = ConvertValueForParameter(record[key]);
						// Ensure we never pass null to NOT NULL columns - use empty string instead
						parameter.Value = parameterValue ?? string.Empty;
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
			if (string.IsNullOrEmpty(jsonData))
			{
				Console.WriteLine("DeserializeData received empty or null JSON data.");
				return new List<Dictionary<string, object>>();
			}

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
							JsonValueKind.Array => property.Value.GetRawText(), // Serialize arrays as JSON
							_ => property.Value.GetRawText() // Fallback for unsupported types
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
