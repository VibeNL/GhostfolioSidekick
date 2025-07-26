using GhostfolioSidekick.Database;
using GhostfolioSidekick.PortfolioViewer.ApiService.Grpc;
using GhostfolioSidekick.PortfolioViewer.Common.SQL;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.ApiService.Services
{
	public class SyncGrpcService : SyncService.SyncServiceBase
	{
		private readonly DatabaseContext _context;
		private readonly ILogger<SyncGrpcService> _logger;
		private readonly string[] _tablesToIgnore = ["sqlite_sequence", "__EFMigrationsHistory", "__EFMigrationsLock"];

		public SyncGrpcService(DatabaseContext context, ILogger<SyncGrpcService> logger)
		{
			_context = context;
			_logger = logger;
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
			if (request.Page <= 0 || request.PageSize <= 0)
			{
				throw new RpcException(new Status(StatusCode.InvalidArgument, "Page and pageSize must be greater than 0."));
			}

			try
			{
				_logger.LogDebug("Getting entity data for {Entity}, page {Page}, page size {PageSize}", 
					request.Entity, request.Page, request.PageSize);

				var result = await RawQuery.ReadTable(_context, request.Entity, request.Page, request.PageSize);

				_logger.LogDebug("Retrieved {RecordCount} records for {Entity}, page {Page}", 
					result.Count, request.Entity, request.Page);

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
				if (records.Count == request.PageSize)
				{
					var nextPageResult = await RawQuery.ReadTable(_context, request.Entity, request.Page + 1, 1);
					hasMore = nextPageResult.Count > 0;
					_logger.LogDebug("Checked next page for {Entity}: hasMore = {HasMore}", request.Entity, hasMore);
				}

				var response = new GetEntityDataResponse
				{
					CurrentPage = request.Page,
					HasMore = hasMore
				};
				response.Records.AddRange(records);

				_logger.LogDebug("Sending response for {Entity}, page {Page}: {RecordCount} records, hasMore: {HasMore}", 
					request.Entity, request.Page, records.Count, hasMore);

				await responseStream.WriteAsync(response);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting entity data for {Entity}", request.Entity);
				throw new RpcException(new Status(StatusCode.Internal, ex.Message));
			}
		}
	}
}