using System.Threading.Tasks;
using Grpc.Core;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.PortfolioViewer.Common.SQL;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.ApiService.Services
{
    public class PortfolioService : Portfolio.PortfolioServiceBase
    {
        private readonly DatabaseContext _context;

        public PortfolioService(DatabaseContext context)
        {
            _context = context;
        }

        public override async Task<SyncResponse> SyncPortfolio(SyncRequest request, ServerCallContext context)
        {
            if (request.Page <= 0 || request.PageSize <= 0)
            {
                return new SyncResponse { Status = "Error", Message = "Page and pageSize must be greater than 0." };
            }

            try
            {
                var result = await RawQuery.ReadTable(_context, request.Entity, request.Page, request.PageSize);
                return new SyncResponse { Status = "Success", Message = "Data synchronized successfully." };
            }
            catch (Exception ex)
            {
                return new SyncResponse { Status = "Error", Message = ex.Message };
            }
        }

        public override async Task<PortfolioDataResponse> GetPortfolioData(PortfolioDataRequest request, ServerCallContext context)
        {
            if (request.Page <= 0 || request.PageSize <= 0)
            {
                return new PortfolioDataResponse { Data = { } };
            }

            try
            {
                var result = await RawQuery.ReadTable(_context, request.Entity, request.Page, request.PageSize);
                var response = new PortfolioDataResponse();
                foreach (var row in result)
                {
                    var data = new PortfolioData();
                    foreach (var column in row)
                    {
                        data.Key = column.Key;
                        data.Value = column.Value.ToString();
                    }
                    response.Data.Add(data);
                }
                return response;
            }
            catch (Exception ex)
            {
                return new PortfolioDataResponse { Data = { } };
            }
        }
    }
}
