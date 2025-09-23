using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.PortfolioViewer.ApiService.Grpc;
using GhostfolioSidekick.PortfolioViewer.Common.SQL;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;
using static Grpc.Core.Metadata;

namespace GhostfolioSidekick.PortfolioViewer.ApiService.Services
{
	public class SyncGrpcService : SyncService.SyncServiceBase
	{
		private readonly DatabaseContext _context;
		private readonly ILogger<SyncGrpcService> _logger;
		private readonly IServerCurrencyConversion _currencyConversion;
		private readonly string[] _tablesToIgnore = ["sqlite_sequence", "__EFMigrationsHistory", "__EFMigrationsLock"];
		
		// Tables that have date columns for partial sync
		private readonly Dictionary<string, string> _tablesWithDates = new()
		{
			{ "Activities", "Date" },
			{ "MarketData", "Date" },
			{ "Balances", "Date" },
			{ "CalculatedSnapshots", "Date" },
			{ "CurrencyExchangeRate", "Date" },
			{ "StockSplits", "Date" },
			{ "CalculatedSnapshotPrimaryCurrency", "Date" },
			{ "BalancesPrimaryCurrency", "Date" },
		};

		public SyncGrpcService(DatabaseContext context, ILogger<SyncGrpcService> logger, IServerCurrencyConversion currencyConversion)
		{
			_context = context;
			_logger = logger;
			_currencyConversion = currencyConversion;
		}

		public override async Task<GetTableNamesResponse> GetTableNames(GetTableNamesRequest request, ServerCallContext context)
		{
			try
			{
				// Use raw ADO.NET to avoid SqlQueryRaw issues with primitive types
				var tableNames = new List<string>();
				
				using var connection = _context.Database.GetDbConnection();
				await connection.OpenAsync(context.CancellationToken);
				using var command = connection.CreateCommand();
				command.CommandText = "SELECT name FROM sqlite_master WHERE type='table'";
				
				using var reader = await command.ExecuteReaderAsync(context.CancellationToken);
				while (await reader.ReadAsync(context.CancellationToken))
				{
					var name = reader.GetString(0);
					if (!string.IsNullOrEmpty(name))
					{
						tableNames.Add(name);
					}
				}
				
				var filteredTableNames = tableNames.Where(x => !_tablesToIgnore.Contains(x)).ToList();

				// Get row counts for each table
				var totalRows = new List<long>();
				foreach (var tableName in filteredTableNames)
				{
					try
					{
						var rowCount = await RawQuery.GetTableCount(_context, tableName);
						totalRows.Add(rowCount);
						_logger.LogDebug("Table {TableName} has {RowCount} rows", tableName, rowCount);
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "Failed to get row count for table {TableName}", tableName);
						totalRows.Add(0); // Default to 0 if we can't get the count
					}
				}

				var response = new GetTableNamesResponse();
				response.TableNames.AddRange(filteredTableNames);
				response.TotalRows.AddRange(totalRows);

				return response;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting table names");
				throw new RpcException(new Status(StatusCode.Internal, ex.Message));
			}
		}

		public override async Task GetEntityData(GetEntityDataRequest request, IServerStreamWriter<GetEntityDataResponse> responseStream, ServerCallContext context)
		{
			await GetEntityDataInternal(request.Entity, request.Page, request.PageSize, null, request.TargetCurrency, responseStream, context);
		}

		public override async Task GetEntityDataSince(GetEntityDataSinceRequest request, IServerStreamWriter<GetEntityDataResponse> responseStream, ServerCallContext context)
		{
			await GetEntityDataInternal(request.Entity, request.Page, request.PageSize, request.SinceDate, request.TargetCurrency, responseStream, context);
		}

		public override async Task<GetLatestDatesResponse> GetLatestDates(GetLatestDatesRequest request, ServerCallContext context)
		{
			try
			{
				var response = new GetLatestDatesResponse();
				
				using var connection = _context.Database.GetDbConnection();
				await connection.OpenAsync(context.CancellationToken);

				foreach (var tableInfo in _tablesWithDates)
				{
					var tableName = tableInfo.Key;
					var dateColumn = tableInfo.Value;

					tableName = await _currencyConversion.ConvertTableNameInCaseOfPrimaryCurrency(tableName);

					try
					{
						using var command = connection.CreateCommand();
						command.CommandText = $"SELECT MAX({dateColumn}) FROM {tableName}";
						
						var result = await command.ExecuteScalarAsync(context.CancellationToken);
						if (result != null && result != DBNull.Value)
						{
							response.LatestDates[tableName] = result.ToString() ?? string.Empty;
							_logger.LogDebug("Latest date in {TableName}: {LatestDate}", tableName, result);
						}
						else
						{
							_logger.LogDebug("No data found in {TableName}", tableName);
						}
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "Failed to get latest date for table {TableName}", tableName);
					}
				}

				return response;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting latest dates");
				throw new RpcException(new Status(StatusCode.Internal, ex.Message));
			}
		}

		private async Task GetEntityDataInternal(string entity, int page, int pageSize, string? sinceDate, string? targetCurrency, IServerStreamWriter<GetEntityDataResponse> responseStream, ServerCallContext context)
		{
			if (page <= 0 || pageSize <= 0)
			{
				throw new RpcException(new Status(StatusCode.InvalidArgument, "Page and pageSize must be greater than 0."));
			}

			entity = await _currencyConversion.ConvertTableNameInCaseOfPrimaryCurrency(entity);

			try
			{
				_logger.LogDebug("Getting entity data for {Entity}, page {Page}, page size {PageSize}, since date {SinceDate}, target currency {TargetCurrency}", 
					entity, page, pageSize, sinceDate ?? "all", targetCurrency ?? "none");

				List<Dictionary<string, object>> result;
				string? dateColumn = null;
				
				if (!string.IsNullOrEmpty(sinceDate) && _tablesWithDates.TryGetValue(entity, out dateColumn))
				{
					// Use date filtering
					result = await GetEntityDataWithDateFilter(entity, dateColumn, sinceDate, page, pageSize);
				}
				else
				{
					// Use regular method
					result = await RawQuery.ReadTable(_context, entity, page, pageSize);
				}

				// Apply server-side currency conversion if target currency is specified
				if (!string.IsNullOrEmpty(targetCurrency))
				{
					try
					{
						var currency = Currency.GetCurrency(targetCurrency);
						result = await _currencyConversion.ConvertTableToPrimaryCurrencyTable(result, entity, currency);
						_logger.LogDebug("Applied currency conversion to {Entity} data for currency {TargetCurrency}", entity, targetCurrency);
					}
					catch (Exception ex)
					{
						_logger.LogWarning(ex, "Failed to apply currency conversion to {Entity} for currency {TargetCurrency}", entity, targetCurrency);
						// Continue without conversion on error
					}
				}

				_logger.LogDebug("Retrieved {RecordCount} records for {Entity}, page {Page}", 
					result.Count, entity, page);

				// Convert the data to protobuf format with proper null handling
				var records = result.Select(record =>
				{
					var entityRecord = new EntityRecord();
					foreach (var kvp in record)
					{
						// Convert all values to strings with proper null/DBNull handling
						// Treat null and DBNull as empty string to prevent NOT NULL constraint violations
						string value = kvp.Value switch
						{
							null => "",
							DBNull => "",
							_ => kvp.Value.ToString() ?? ""
						};
						entityRecord.Fields[kvp.Key] = value;
					}
					return entityRecord;
				}).ToList();

				// Check if there are more records by trying to get the next page with a limit of 1
				bool hasMore = false;
				if (records.Count == pageSize)
				{
					List<Dictionary<string, object>> nextPageResult;
					if (!string.IsNullOrEmpty(sinceDate) && dateColumn != null)
					{
						nextPageResult = await GetEntityDataWithDateFilter(entity, dateColumn, sinceDate, page + 1, 1);
					}
					else
					{
						nextPageResult = await RawQuery.ReadTable(_context, entity, page + 1, 1);
					}
					hasMore = nextPageResult.Count > 0;
					_logger.LogDebug("Checked next page for {Entity}: hasMore = {HasMore}", entity, hasMore);
				}

				var response = new GetEntityDataResponse
				{
					CurrentPage = page,
					HasMore = hasMore
				};
				response.Records.AddRange(records);

				_logger.LogDebug("Sending response for {Entity}, page {Page}: {RecordCount} records, hasMore: {HasMore}", 
					entity, page, records.Count, hasMore);

				await responseStream.WriteAsync(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting entity data for {Entity}", entity);
				throw new RpcException(new Status(StatusCode.Internal, ex.Message));
			}
		}

		private async Task<List<Dictionary<string, object>>> GetEntityDataWithDateFilter(string entity, string dateColumn, string sinceDate, int page, int pageSize)
		{
			var offset = (page - 1) * pageSize;
			
			// Validate table and column names to prevent SQL injection
			if (!_tablesWithDates.ContainsKey(entity))
			{
				throw new ArgumentException($"Table {entity} is not supported for date filtering");
			}
			
			var sqlQuery = $"SELECT * FROM {entity} WHERE {dateColumn} >= @sinceDate ORDER BY {dateColumn} LIMIT @pageSize OFFSET @offset";

			using var connection = _context.Database.GetDbConnection();
			await connection.OpenAsync();
			using var command = connection.CreateCommand();
			command.CommandText = sqlQuery;

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
			offsetParam.Value = offset;
			command.Parameters.Add(offsetParam);

			using var reader = await command.ExecuteReaderAsync();

			var result = new List<Dictionary<string, object>>();
			while (await reader.ReadAsync())
			{
				var row = new Dictionary<string, object>();
				for (var i = 0; i < reader.FieldCount; i++)
				{
					row[reader.GetName(i)] = reader.IsDBNull(i) ? null : reader.GetValue(i);
				}
				result.Add(row);
			}

			return result;
		}
	}
}