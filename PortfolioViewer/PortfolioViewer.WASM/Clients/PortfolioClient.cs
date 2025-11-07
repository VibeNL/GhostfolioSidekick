using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.PortfolioViewer.ApiService.Grpc;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using Grpc.Net.Client;
using Grpc.Net.Client.Web;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text.Json;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Clients
{
	public class PortfolioClient(
		HttpClient httpClient,
		SqlitePersistence sqlitePersistence,
		ICurrencyExchange currencyExchange,
		IDbContextFactory<DatabaseContext> dbContextFactory,
		ISyncTrackingService syncTrackingService,
		ILogger<PortfolioClient> logger) : IDisposable
	{
		private readonly string[] TablesToIgnore = ["sqlite_sequence", "__EFMigrationsHistory", "__EFMigrationsLock"];

		const int pageSize = 10_000;
		const int PartialSyncBufferDays = 1; // Buffer to ensure we don't miss any data

		private GrpcChannel? _grpcChannel;
		private SyncService.SyncServiceClient? _grpcClient;
		private bool _disposed;

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2139:Exceptions should be either logged or rethrown but not both", Justification = "Update UI")]
		public async Task SyncPortfolio(IProgress<(string action, int progress)> progress, bool forceFullSync, CancellationToken cancellationToken = default)
		{
			try
			{
				// Step 0: Ensure Database is Up-to-Date
				await using var databaseContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);
				var pendingMigrations = await databaseContext.Database.GetPendingMigrationsAsync(cancellationToken: cancellationToken);
				if (pendingMigrations.Any())
				{
					try
					{
						await databaseContext.Database.MigrateAsync(cancellationToken: cancellationToken);
					}
					catch (Exception ex)
					{
						progress.Report(("Error applying database migrations. Forcing new database.", 0));
						logger.LogWarning(ex, "Failed to apply migrations, forcing new database");
						forceFullSync = true;

						await databaseContext.Database.EnsureDeletedAsync(cancellationToken);
						await databaseContext.Database.MigrateAsync(cancellationToken: cancellationToken);
					}
				}

				var grpcClient = GetGrpcClient();

				// Check if we should do a partial sync (only if not forced to do full sync)
				if (!forceFullSync)
				{
					var lastSyncTime = await syncTrackingService.GetLastSyncTimeAsync();
					var hasEverSynced = await syncTrackingService.HasEverSyncedAsync();

					if (hasEverSynced && lastSyncTime.HasValue)
					{
						progress?.Report(("Checking for partial sync possibility...", 0));

						var partialSyncSuccess = await TryPartialSync(grpcClient, lastSyncTime.Value, progress, cancellationToken);
						if (partialSyncSuccess)
						{
							await ReloadCached();
							progress?.Report(("Partial sync completed successfully.", 100));
							return;
						}
						else
						{
							progress?.Report(("Partial sync not possible, falling back to full sync...", 0));
							logger.LogInformation("Partial sync failed or not beneficial, performing full sync");
						}
					}
				}
				else
				{
					logger.LogInformation("Full sync requested by user, skipping partial sync check");
					progress?.Report(("Performing full sync as requested...", 0));
				}

				await PerformFullSync(grpcClient, progress, cancellationToken);
				await ReloadCached();
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error during portfolio sync: {Message}", ex.Message);
				progress?.Report(($"Error: {ex.Message}", 100));
				throw;
			}
		}

		private async Task ReloadCached()
		{
			await currencyExchange.ClearCache();
			await currencyExchange.PreloadAllExchangeRates();
		}

		private async Task<bool> TryPartialSync(SyncService.SyncServiceClient grpcClient, DateTime lastSyncTime, IProgress<(string action, int progress)>? progress, CancellationToken cancellationToken)
		{
			try
			{
				// Add buffer days to ensure we don't miss any data
				var sinceDate = lastSyncTime.Date.AddDays(-PartialSyncBufferDays);
				// Use culture-invariant ISO 8601 date format consistently
				var sinceDateString = sinceDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

				progress?.Report(($"Checking data since {sinceDateString}...", 5));

				await using var databaseContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

				// Get all table names dynamically from the database
				var allTables = await GetDatabaseTablesAsync(databaseContext);
				var tablesWithDateColumns = await GetTablesWithDateColumnsAsync(databaseContext, allTables);
				var tablesWithoutDateColumns = allTables.Except(tablesWithDateColumns).ToList();

				// Get latest dates from server for tables with date columns
				var latestDatesResponse = await grpcClient.GetLatestDatesAsync(new GetLatestDatesRequest(), cancellationToken: cancellationToken);

				// Check if partial sync would be beneficial for tables with dates
				var tablesToSyncPartially = tablesWithDateColumns
					.Where(table => latestDatesResponse.LatestDates.TryGetValue(table, out string? latestDateStr)
								   && DateTime.TryParseExact(latestDateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var latestDate)
								   && latestDate >= sinceDate)
					.ToList();

				var tablesToSyncFully = new List<string>(tablesWithoutDateColumns);

				// Always sync tables without date columns during partial sync
				// If no tables need syncing, we're up to date
				if (tablesToSyncPartially.Count == 0 && tablesToSyncFully.Count == 0)
				{
					progress?.Report(("No new data found, sync complete.", 100));
					return true;
				}

				var totalTablesToSync = tablesToSyncPartially.Count + tablesToSyncFully.Count;
				progress?.Report(($"Performing partial sync for {totalTablesToSync} tables since {sinceDateString}...", 10));

				// Enable performance optimizations
				await databaseContext.ExecutePragma("PRAGMA foreign_keys=OFF;");
				await databaseContext.ExecutePragma("PRAGMA synchronous=OFF;");
				await databaseContext.ExecutePragma("PRAGMA journal_mode=MEMORY;");

				var totalProgress = 0;
				var progressStep = 80 / totalTablesToSync; // Reserve 20% for cleanup

				// Sync tables with date columns (partial sync)
				foreach (var tableName in tablesToSyncPartially)
				{
					progress?.Report(($"Syncing new data for table: {tableName}...", 10 + totalProgress));

					// Delete existing data since the sync date to avoid duplicates
					var deleteRecordsCount = await DeleteRecordsSinceDate(databaseContext, tableName, sinceDateString, cancellationToken);
					logger.LogInformation("Deleted {RecordCount} existing records from {TableName} since {SinceDate}", deleteRecordsCount, tableName, sinceDateString);

					// Sync new data with server-side currency conversion
					await SyncTableDataSince(grpcClient, databaseContext, tableName, sinceDateString, cancellationToken);

					totalProgress += progressStep;
				}

				// Sync tables without date columns (full sync for these tables)
				foreach (var tableName in tablesToSyncFully)
				{
					progress?.Report(($"Syncing all data for table: {tableName}...", 10 + totalProgress));

					// Clear all data for tables without date columns
					var deleteSql = $"DELETE FROM {tableName}";
					await databaseContext.ExecuteSqlRawAsync(deleteSql, cancellationToken);

					// Sync all data for this table
					await SyncTableDataFull(grpcClient, databaseContext, tableName, cancellationToken);

					totalProgress += progressStep;
				}

				// Re-enable constraints and finalize
				await databaseContext.ExecutePragma("PRAGMA foreign_keys=ON;");
				await databaseContext.ExecutePragma("PRAGMA synchronous=FULL;");
				await databaseContext.ExecutePragma("PRAGMA journal_mode=DELETE;");

				await sqlitePersistence.SaveChangesAsync();

				var allSyncedTables = tablesToSyncPartially.Concat(tablesToSyncFully).ToList();
				logger.LogInformation("Partial sync completed successfully for {TableCount} tables: {Tables}",
					allSyncedTables.Count, string.Join(", ", allSyncedTables));
				return true;
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Partial sync failed: {Message}", ex.Message);
				return false;
			}
		}

		private async Task<List<string>> GetDatabaseTablesAsync(DatabaseContext databaseContext)
		{
			var query = "SELECT name FROM sqlite_master WHERE type='table' AND name NOT IN (@ignore1, @ignore2, @ignore3)";

			using var connection = databaseContext.Database.GetDbConnection();
			await connection.OpenAsync();
			using var command = connection.CreateCommand();
			command.CommandText = query;

			// Add parameters for tables to ignore using LINQ
			var parameters = TablesToIgnore.Select((ignore, index) =>
			{
				var param = command.CreateParameter();
				param.ParameterName = $"@ignore{index + 1}";
				param.Value = ignore;
				return param;
			}).ToArray();

			command.Parameters.AddRange(parameters);

			var tables = new List<string>();
			using var reader = await command.ExecuteReaderAsync();
			while (await reader.ReadAsync())
			{
				tables.Add(reader.GetString(0));
			}

			return tables;
		}

		private static async Task<List<string>> GetTablesWithDateColumnsAsync(DatabaseContext databaseContext, List<string> tables)
		{
			var tablesWithDateColumns = new List<string>();

			using var connection = databaseContext.Database.GetDbConnection();
			await connection.OpenAsync();

			foreach (var table in tables)
			{
				// Check if table has a "Date" column
				using var command = connection.CreateCommand();
				command.CommandText = $"PRAGMA table_info({table})";

				using var reader = await command.ExecuteReaderAsync();
				while (await reader.ReadAsync())
				{
					var columnName = reader.GetString(1); // Column name is at index 1
					if (string.Equals(columnName, "Date", StringComparison.OrdinalIgnoreCase))
					{
						tablesWithDateColumns.Add(table);
						break; // Found the Date column, no need to check further
					}
				}
			}

			return tablesWithDateColumns;
		}

		private static async Task<int> DeleteRecordsSinceDate(DatabaseContext databaseContext, string tableName, string sinceDateString, CancellationToken cancellationToken)
		{
			// Check if table has a Date column
			var hasDateColumn = await TableHasDateColumn(databaseContext, tableName);
			if (!hasDateColumn)
			{
				return 0;
			}

			var deleteSql = $"DELETE FROM {tableName} WHERE Date >= @sinceDate";

			using var connection = databaseContext.Database.GetDbConnection();
			await connection.OpenAsync(cancellationToken);
			using var command = connection.CreateCommand();
			command.CommandText = deleteSql;

			var sinceDateParam = command.CreateParameter();
			sinceDateParam.ParameterName = "@sinceDate";
			sinceDateParam.Value = sinceDateString;
			command.Parameters.Add(sinceDateParam);

			return await command.ExecuteNonQueryAsync(cancellationToken);
		}

		private static async Task<bool> TableHasDateColumn(DatabaseContext databaseContext, string tableName)
		{
			using var connection = databaseContext.Database.GetDbConnection();
			await connection.OpenAsync();
			using var command = connection.CreateCommand();
			command.CommandText = $"PRAGMA table_info({tableName})";

			using var reader = await command.ExecuteReaderAsync();
			while (await reader.ReadAsync())
			{
				var columnName = reader.GetString(1); // Column name is at index 1
				if (string.Equals(columnName, "Date", StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}

			return false;
		}

		private async Task SyncTableDataFull(SyncService.SyncServiceClient grpcClient, DatabaseContext databaseContext, string tableName, CancellationToken cancellationToken)
		{
			await foreach (var dataChunk in FetchDataAsync(grpcClient, tableName, cancellationToken))
			{
				await InsertDataAsync(databaseContext, tableName, dataChunk, cancellationToken);
			}
		}

		private async Task SyncTableDataSince(SyncService.SyncServiceClient grpcClient, DatabaseContext databaseContext, string tableName, string sinceDateString, CancellationToken cancellationToken)
		{
			await foreach (var dataChunk in FetchDataSinceAsync(grpcClient, tableName, sinceDateString, cancellationToken))
			{
				await InsertDataAsync(databaseContext, tableName, dataChunk, cancellationToken);
			}
		}

		private async IAsyncEnumerable<List<Dictionary<string, object>>> FetchDataSinceAsync(SyncService.SyncServiceClient grpcClient, string tableName, string sinceDateString, [EnumeratorCancellation] CancellationToken cancellationToken)
		{
			int page = 1;
			bool hasMore = true;

			logger.LogInformation("Starting to fetch data for table: {TableName} since {SinceDate}", tableName, sinceDateString);

			while (hasMore)
			{
				logger.LogDebug("Fetching page {Page} for table {TableName} since {SinceDate} with page size {PageSize}", page, tableName, sinceDateString, pageSize);

				var request = new GetEntityDataSinceRequest
				{
					Entity = tableName,
					Page = page,
					PageSize = pageSize,
					SinceDate = sinceDateString,
				};

				using var call = grpcClient.GetEntityDataSince(request, cancellationToken: cancellationToken);

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

					// Convert gRPC response to the expected format with LINQ
					var data = response.Records.Select(record =>
						record.Fields.ToDictionary(field => field.Key, field => ConvertStringToValue(field.Value))
					).ToList();

					yield return data;

					// Check if there's more data based on the response
					hasMore = response.HasMore;
				}

				if (!receivedData)
				{
					logger.LogDebug("No data received for page {Page} of table {TableName} since {SinceDate}", page, tableName, sinceDateString);
					hasMore = false;
				}

				page++;
			}

			logger.LogInformation("Completed fetching data for table: {TableName} since {SinceDate}, total pages: {TotalPages}", tableName, sinceDateString, page - 1);
		}

		private async Task PerformFullSync(SyncService.SyncServiceClient grpcClient, IProgress<(string action, int progress)>? progress, CancellationToken cancellationToken)
		{
			// Step 1: Retrieve Table Names and Row Counts
			progress?.Report(("Retrieving table names and row counts...", 0));
			var tableNamesResponse = await grpcClient.GetTableNamesAsync(new GetTableNamesRequest(), cancellationToken: cancellationToken);
			var tableNames = tableNamesResponse.TableNames.ToList();
			var totalRows = tableNamesResponse.TotalRows.ToList();

			if (tableNames.Count == 0)
			{
				progress?.Report(("No tables found in the database.", 100));
				return;
			}

			var recordsWritten = 0;
			var totalRecordsToSync = totalRows.Sum();

			await using var databaseContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

			// Step 2: Clear Tables
			// Disable contraints on DB
			await databaseContext.ExecutePragma("PRAGMA foreign_keys=OFF;");
			await databaseContext.ExecutePragma("PRAGMA synchronous=OFF;");
			await databaseContext.ExecutePragma("PRAGMA journal_mode=MEMORY;");
			await databaseContext.ExecutePragma("PRAGMA cache_size =1000000;");
			await databaseContext.ExecutePragma("PRAGMA locking_mode=EXCLUSIVE;");
			await databaseContext.ExecutePragma("PRAGMA temp_store =MEMORY;");
			await databaseContext.ExecutePragma("PRAGMA auto_vacuum=0;");

			// Clear all non-ignored tables using LINQ
			var tablesToClear = tableNames.Zip(totalRows, (name, count) => new { Name = name, Count = count })
				.Where(t => !TablesToIgnore.Contains(t.Name));

			foreach (var table in tablesToClear)
			{
				progress?.Report(($"Clearing table: {table.Name} ({table.Count} rows)...", 0));
				var deleteSql = $"DELETE FROM {table.Name}";
				await databaseContext.ExecuteSqlRawAsync(deleteSql, cancellationToken);
			}

			// Step 3: Sync Data for Each Table with server-side currency conversion
			var syncTables = tableNames.Zip(totalRows, (name, count) => new { Name = name, Count = count })
				.Where(t => !TablesToIgnore.Contains(t.Name));

			foreach (var table in syncTables)
			{
				var totalWrittenTable = 0;
				progress?.Report(($"Syncing data for table: {table.Name} ({table.Count} rows)...", CalculatePercentage(recordsWritten, totalRecordsToSync)));

				await foreach (var dataChunk in FetchDataAsync(grpcClient, table.Name, cancellationToken))
				{
					progress?.Report(($"Inserting data into table: {table.Name} ({totalWrittenTable}/{table.Count})...", CalculatePercentage(recordsWritten, totalRecordsToSync)));
					await InsertDataAsync(databaseContext, table.Name, dataChunk, cancellationToken);
					totalWrittenTable += dataChunk.Count;
					recordsWritten += dataChunk.Count;
					progress?.Report(($"Inserted {totalWrittenTable}/{table.Count} records into table: {table.Name}...", CalculatePercentage(recordsWritten, totalRecordsToSync)));
				}

				logger.LogInformation("Completed syncing table {TableName}: {ActualRows}/{ExpectedRows} records",
					table.Name, totalWrittenTable, table.Count);
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

			await sqlitePersistence.SaveChangesAsync();

			progress?.Report(("Full sync completed successfully.", 100));
		}

		private SyncService.SyncServiceClient GetGrpcClient()
		{
			if (_grpcClient != null)
			{
				return _grpcClient;
			}

			// Create gRPC channel for web - use the httpClient's base address but ensure it's absolute
			var baseAddress = httpClient.BaseAddress ?? throw new InvalidOperationException("HttpClient BaseAddress is not configured.");

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

					// Convert gRPC response to the expected format with LINQ
					var data = response.Records.Select(record =>
						record.Fields.ToDictionary(field => field.Key, field => ConvertStringToValue(field.Value))
					).ToList();

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
			var columns = string.Join(", ", dataChunk[0].Keys.Select(key => $"\"{key}\""));
			using var connection = databaseContext.Database.GetDbConnection();
			using var command = connection.CreateCommand();
			var parametersString = string.Join(", ", dataChunk[0].Keys.Select(key => $"${key}"));
			command.CommandText = $"INSERT INTO \"{tableName}\" ({columns}) VALUES ({parametersString})";

			var parameters = dataChunk[0].Keys.ToDictionary(
				key => key,
				key => new Microsoft.Data.Sqlite.SqliteParameter($"${key}", 0)
			);
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
				return [];
			}

			Console.WriteLine($"DeserializeData executing");
			var stopwatch = System.Diagnostics.Stopwatch.StartNew(); // Start timing

			if (string.IsNullOrWhiteSpace(jsonData))
			{
				return [];
			}

			try
			{
				// Use JsonDocument for efficient parsing
				using var document = JsonDocument.Parse(jsonData);

				// Use LINQ to convert JSON to dictionary list
				var result = document.RootElement.EnumerateArray()
					.Select(element => element.EnumerateObject()
						.ToDictionary(
							property => property.Name,
							property => (object)(property.Value.ValueKind switch
							{
								JsonValueKind.String => property.Value.GetString() ?? string.Empty,
								JsonValueKind.Number => property.Value.TryGetInt64(out var longValue) ? longValue : property.Value.GetDouble(),
								JsonValueKind.True => true,
								JsonValueKind.False => false,
								JsonValueKind.Null => DBNull.Value,
								JsonValueKind.Object => property.Value.GetRawText(), // Serialize nested objects as JSON
								JsonValueKind.Array => property.Value.GetRawText(), // Serialize arrays as JSON
								_ => property.Value.GetRawText() // Fallback for unsupported types
							})
						)
					)
					.ToList();

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

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S2139:Exceptions should be either logged or rethrown but not both", Justification = "UI Update")]
		public async Task DeleteAllData(IProgress<(string action, int progress)> progress, CancellationToken cancellationToken = default)
		{
			try
			{
				progress?.Report(("Deleting all database data...", 10));
				logger.LogInformation("Starting database deletion");

				await using var databaseContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

				// Step 1: Delete the database file completely
				progress?.Report(("Deleting database file...", 30));
				await databaseContext.Database.EnsureDeletedAsync(cancellationToken);

				// Step 2: Recreate the database with migrations
				progress?.Report(("Recreating database structure...", 60));
				await databaseContext.Database.MigrateAsync(cancellationToken: cancellationToken);

				// Step 3: Clear sync tracking
				progress?.Report(("Clearing sync history...", 80));
				await syncTrackingService.ClearSyncTimeAsync();

				// Step 4: Clear IndexedDB storage
				progress?.Report(("Clearing browser storage...", 85));
				try
				{
					await sqlitePersistence.ClearDatabaseFromIndexedDb();
				}
				catch (Exception ex)
				{
					logger.LogWarning(ex, "Failed to clear IndexedDB storage, continuing with deletion");
				}

				// Step 5: Clear cache
				progress?.Report(("Clearing cache...", 90));
				await currencyExchange.ClearCache();

				progress?.Report(("Database deletion completed successfully.", 100));
				logger.LogInformation("Database deletion completed successfully");
			}
			catch (Exception ex)
			{
				logger.LogError(ex, "Error during database deletion: {Message}", ex.Message);
				progress?.Report(($"Error deleting database: {ex.Message}", 100));
				throw;
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

		private static int CalculatePercentage(long written, long total)
		{
			if (total == 0)
			{
				return 100; // Avoid division by zero
			}

			return (int)((double)written / total * 100);
		}

		public async Task SyncSingleTable(string tableName, IProgress<(string action, int progress)> progress, CancellationToken cancellationToken = default)
		{
			try
			{
				progress?.Report(($"Starting sync for table: {tableName}...", 10));

				var grpcClient = GetGrpcClient();
				
				await using var databaseContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

				// Enable performance optimizations
				await databaseContext.ExecutePragma("PRAGMA foreign_keys=OFF;");
				await databaseContext.ExecutePragma("PRAGMA synchronous=OFF;");
				await databaseContext.ExecutePragma("PRAGMA journal_mode=MEMORY;");

				progress?.Report(($"Clearing existing data in table: {tableName}...", 20));

				// Clear existing data for the table
				var deleteSql = $"DELETE FROM {tableName}";
				await databaseContext.ExecuteSqlRawAsync(deleteSql, cancellationToken);

				progress?.Report(($"Fetching latest data for table: {tableName}...", 40));

				// Sync all data for this table
				await foreach (var dataChunk in FetchDataAsync(grpcClient, tableName, cancellationToken))
				{
					progress?.Report(($"Inserting data into table: {tableName}...", 70));
					await InsertDataAsync(databaseContext, tableName, dataChunk, cancellationToken);
				}

				progress?.Report(($"Finalizing sync for table: {tableName}...", 90));

				// Re-enable constraints and finalize
				await databaseContext.ExecutePragma("PRAGMA foreign_keys=ON;");
				await databaseContext.ExecutePragma("PRAGMA synchronous=FULL;");
				await databaseContext.ExecutePragma("PRAGMA journal_mode=DELETE;");

				await sqlitePersistence.SaveChangesAsync();

				progress?.Report(($"Sync completed for table: {tableName}.", 100));
				logger.LogInformation("Single table sync completed successfully for table: {TableName}", tableName);
			}
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
			catch (Exception ex)
			{
				logger.LogError(ex, "Error during single table sync for {TableName}: {Message}", tableName, ex.Message);
				progress?.Report(($"Error syncing table {tableName}: {ex.Message}", 100));
				throw;
			}
#pragma warning restore S2139 // Exceptions should be either logged or rethrown but not both
		}
	}
}
