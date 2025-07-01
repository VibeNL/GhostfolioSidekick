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
				var tables = _context.SqlQueryRaw<string>("SELECT name FROM sqlite_master WHERE type='table'");
				var tableNames = await tables.ToListAsync(context.CancellationToken);
				
				var filteredTableNames = tableNames.Where(x => !_tablesToIgnore.Contains(x)).ToList();

				var response = new GetTableNamesResponse();
				response.TableNames.AddRange(filteredTableNames);

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
				var result = await RawQuery.ReadTable(_context, request.Entity, request.Page, request.PageSize);

				// Convert the data to protobuf format
				var records = result.Select(record =>
				{
					var entityRecord = new EntityRecord();
					foreach (var kvp in record)
					{
						// Convert all values to strings for simplicity in protobuf
						entityRecord.Fields[kvp.Key] = kvp.Value?.ToString() ?? "";
					}
					return entityRecord;
				}).ToList();

				// For simplicity, we'll send all records in one response
				// In a real implementation, you might want to stream in chunks
				var response = new GetEntityDataResponse
				{
					CurrentPage = request.Page,
					HasMore = records.Count == request.PageSize // Simple heuristic
				};
				response.Records.AddRange(records);

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