using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Services
{
    public class UpcomingDividendsService : IUpcomingDividendsService
    {
        public Task<List<UpcomingDividendModel>> GetUpcomingDividendsAsync()
        {
            // Mock/sample data
            var data = new List<UpcomingDividendModel>
            {
                new UpcomingDividendModel
                {
                    Symbol = "AAPL",
                    CompanyName = "Apple Inc.",
                    ExDate = DateTime.Today.AddDays(5),
                    PaymentDate = DateTime.Today.AddDays(20),
                    Amount = 0.24m,
                    Currency = "USD"
                },
                new UpcomingDividendModel
                {
                    Symbol = "MSFT",
                    CompanyName = "Microsoft Corp.",
                    ExDate = DateTime.Today.AddDays(10),
                    PaymentDate = DateTime.Today.AddDays(25),
                    Amount = 0.68m,
                    Currency = "USD"
                },
                new UpcomingDividendModel
                {
                    Symbol = "VZ",
                    CompanyName = "Verizon Communications",
                    ExDate = DateTime.Today.AddDays(3),
                    PaymentDate = DateTime.Today.AddDays(18),
                    Amount = 0.65m,
                    Currency = "USD"
                }
            };
            return Task.FromResult(data);
        }
    }
}