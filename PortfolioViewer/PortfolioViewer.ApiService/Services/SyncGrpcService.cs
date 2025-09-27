using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.PortfolioViewer.ApiService.Grpc;
using GhostfolioSidekick.PortfolioViewer.Common.SQL;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.ApiService.Services
{
	public class SyncGrpcService(DatabaseContext dbContext, ILogger<SyncGrpcService> logger) : SyncService.SyncServiceBase
	{
		private readonly string[] _tablesToIgnore = ["sqlite_sequence", "__EFMigrationsHistory", "__EFMigrationsLock"];
		private Dictionary<string, string> _tablesWithDates = [];
		private bool _tablesWithDatesInitialized;

		private async Task<Dictionary<string, string>> GetTablesWithDatesAsync()
		{
			if (_tablesWithDatesInitialized) return _tablesWithDates;

			using var connection = dbContext.Database.GetDbConnection();
			await connection.OpenAsync();
			
			var tableNames = new List<string>();
			using (var cmd = connection.CreateCommand()) {
				cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table'";
				using var reader = await cmd.ExecuteReaderAsync();
				while (await reader.ReadAsync()) {
					var name = reader.GetString(0);
					if (!_tablesToIgnore.Contains(name)) tableNames.Add(name);
				}
			}
			
			foreach (var tableName in tableNames) {
				try {
					using var cmd = connection.CreateCommand();
					cmd.CommandText = $"PRAGMA table_info({tableName})";
					using var reader = await cmd.ExecuteReaderAsync();
					while (await reader.ReadAsync()) {
						var columnName = reader.GetString(1);
						var columnType = reader.GetString(2);
						if (IsDateColumn(columnName, columnType)) {
							_tablesWithDates[tableName] = columnName;
							logger.LogDebug("Found date column {ColumnName} in table {TableName}", columnName, tableName);
							break;
						}
					}
				} catch (Exception ex) {
					logger.LogWarning(ex, "Failed to analyze columns for table {TableName}", tableName);
				}
			}
			
			_tablesWithDatesInitialized = true;
			logger.LogInformation("Discovered {Count} tables with date columns: {Tables}", 
				_tablesWithDates.Count, string.Join(", ", _tablesWithDates.Select(kvp => $"{kvp.Key}({kvp.Value})")));
			
			return _tablesWithDates;
		}

		private static bool IsDateColumn(string columnName, string columnType) =>
			columnName.ToLower() switch {
				var name when name == "date" || name.EndsWith("date") || name.StartsWith("date") || name.Contains("timestamp") => true,
				_ => columnType.ToLower() switch {
					var type when type.Contains("date") || type.Contains("datetime") || type.Contains("timestamp") => true,
					_ => false
				}
			};

		public override async Task<GetTableNamesResponse> GetTableNames(GetTableNamesRequest request, ServerCallContext context)
		{
			try {
				await GetTablesWithDatesAsync();
				
				var tableNames = new List<string>();
				using var connection = dbContext.Database.GetDbConnection();
				await connection.OpenAsync(context.CancellationToken);
				using (var command = connection.CreateCommand()) {
					command.CommandText = "SELECT name FROM sqlite_master WHERE type='table'";
					using var reader = await command.ExecuteReaderAsync(context.CancellationToken);
					while (await reader.ReadAsync(context.CancellationToken)) {
						var name = reader.GetString(0);
						if (!string.IsNullOrEmpty(name)) tableNames.Add(name);
					}
				}	
				
				var filteredTableNames = tableNames.Where(x => !_tablesToIgnore.Contains(x)).ToList();
				var totalRows = new List<long>();
				
				foreach (var tableName in filteredTableNames) {
					try {
						totalRows.Add(await RawQuery.GetTableCount(dbContext, tableName));
						logger.LogDebug("Table {TableName} has {RowCount} rows", tableName, totalRows.Last());
					} catch (Exception ex) {
						logger.LogWarning(ex, "Failed to get row count for table {TableName}", tableName);
						totalRows.Add(0);
					}
				}

				return new GetTableNamesResponse { TableNames = { filteredTableNames }, TotalRows = { totalRows } };
			} catch (Exception ex) {
				logger.LogError(ex, "Error getting table names");
				throw new RpcException(new Status(StatusCode.Internal, ex.Message));
			}
		}

		public override async Task GetEntityData(GetEntityDataRequest request, IServerStreamWriter<GetEntityDataResponse> responseStream, ServerCallContext context) =>
			await GetEntityDataInternal(request.Entity, request.Page, request.PageSize, null, responseStream, context);

		public override async Task GetEntityDataSince(GetEntityDataSinceRequest request, IServerStreamWriter<GetEntityDataResponse> responseStream, ServerCallContext context) =>
			await GetEntityDataInternal(request.Entity, request.Page, request.PageSize, request.SinceDate, responseStream, context);

		public override async Task<GetLatestDatesResponse> GetLatestDates(GetLatestDatesRequest request, ServerCallContext context)
		{
			try {
				var tablesWithDates = await GetTablesWithDatesAsync();
				var response = new GetLatestDatesResponse();
				
				using var connection = dbContext.Database.GetDbConnection();
				await connection.OpenAsync(context.CancellationToken);

				foreach (var tableInfo in tablesWithDates) {
					var tableName = tableInfo.Key;
					var dateColumn = tableInfo.Value;
					
					try {
						using var command = connection.CreateCommand();
						command.CommandText = $"SELECT MAX({dateColumn}) FROM {tableName}";
						var result = await command.ExecuteScalarAsync(context.CancellationToken);
						if (result is not null and not DBNull) {
							response.LatestDates[tableName] = result.ToString() ?? string.Empty;
							logger.LogDebug("Latest date in {TableName}: {LatestDate}", tableName, result);
						} else {
							logger.LogDebug("No data found in {TableName}", tableName);
						}
					} catch (Exception ex) {
						logger.LogWarning(ex, "Failed to get latest date for table {TableName}", tableName);
					}
				}

				return response;
			} catch (Exception ex) {
				logger.LogError(ex, "Error getting latest dates");
				throw new RpcException(new Status(StatusCode.Internal, ex.Message));
			}
		}

		private async Task GetEntityDataInternal(string entity, int page, int pageSize, string? sinceDate, IServerStreamWriter<GetEntityDataResponse> responseStream, ServerCallContext context)
		{
			if (page <= 0 || pageSize <= 0)
				throw new RpcException(new Status(StatusCode.InvalidArgument, "Page and pageSize must be greater than 0."));

			try {
				var tablesWithDates = await GetTablesWithDatesAsync();
				logger.LogDebug("Getting entity data for {Entity}, page {Page}, page size {PageSize}, since date {SinceDate}", 
					entity, page, pageSize, sinceDate ?? "all");

				string? dateColumn = null;
				var result = !string.IsNullOrEmpty(sinceDate) && tablesWithDates.TryGetValue(entity, out dateColumn)
					? await GetEntityDataWithDateFilter(entity, dateColumn, sinceDate, page, pageSize)
					: await RawQuery.ReadTable(dbContext, entity, page, pageSize);

				logger.LogDebug("Retrieved {RecordCount} records for {Entity}, page {Page}", result.Count, entity, page);

				var records = result.Select(record => {
					var entityRecord = new EntityRecord();
					foreach (var kvp in record)
						entityRecord.Fields[kvp.Key] = kvp.Value switch { null or DBNull => "", _ => kvp.Value.ToString() ?? "" };
					return entityRecord;
				}).ToList();

				var hasMore = records.Count == pageSize && (
					!string.IsNullOrEmpty(sinceDate) && dateColumn != null
						? await GetEntityDataWithDateFilter(entity, dateColumn, sinceDate, page + 1, 1)
						: await RawQuery.ReadTable(dbContext, entity, page + 1, 1)
				).Count > 0;

				logger.LogDebug("Checked next page for {Entity}: hasMore = {HasMore}", entity, hasMore);

				await responseStream.WriteAsync(new GetEntityDataResponse { 
					CurrentPage = page, 
					HasMore = hasMore, 
					Records = { records } 
				});

				logger.LogDebug("Sending response for {Entity}, page {Page}: {RecordCount} records, hasMore: {HasMore}", 
					entity, page, records.Count, hasMore);
			} catch (Exception ex) {
				logger.LogError(ex, "Error getting entity data for {Entity}", entity);
				throw new RpcException(new Status(StatusCode.Internal, ex.Message));
			}
		}

		private async Task<List<Dictionary<string, object>>> GetEntityDataWithDateFilter(string entity, string dateColumn, string sinceDate, int page, int pageSize)
		{
			var tablesWithDates = await GetTablesWithDatesAsync();
			if (!tablesWithDates.ContainsKey(entity))
				throw new ArgumentException($"Table {entity} is not supported for date filtering");
			
			using var connection = dbContext.Database.GetDbConnection();
			await connection.OpenAsync();
			using var command = connection.CreateCommand();
			command.CommandText = $"SELECT * FROM {entity} WHERE {dateColumn} >= @sinceDate ORDER BY {dateColumn} LIMIT @pageSize OFFSET @offset";

			var sinceDateParam = command.CreateParameter();
			sinceDateParam.ParameterName = "@sinceDate";	
			sinceDateParam.Value = sinceDate;
			command.Parameters.Add(sinceDateParam);

			var pageSizeParam = command.CreateParameter();
			pageSizeParam.ParameterName = "@pageSize";
			pageSizeParam.Value = pageSize;
			command.Parameters.Add(pageSizeParam);

			var offsetParam = command.CreateParameter();
			offsetParam.ParameterName = "@offset";
			offsetParam.Value = (page - 1) * pageSize;
			command.Parameters.Add(offsetParam);

			using var reader = await command.ExecuteReaderAsync();
			var result = new List<Dictionary<string, object>>();
			while (await reader.ReadAsync()) {
				var row = new Dictionary<string, object>();
				for (var i = 0; i < reader.FieldCount; i++)
					row[reader.GetName(i)] = reader.IsDBNull(i) ? null! : reader.GetValue(i);
				result.Add(row);
			}
			return result;
		}
	}
}