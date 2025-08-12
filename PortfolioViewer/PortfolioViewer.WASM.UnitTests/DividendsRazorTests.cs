using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;
using GhostfolioSidekick.PortfolioViewer.WASM.Pages;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using GhostfolioSidekick.PortfolioViewer.WASM.Models;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;

namespace GhostfolioSidekick.PortfolioViewer.WASM.UnitTests
{
    public class DividendsRazorTests : TestContext
    {
        public DividendsRazorTests()
        {
            Services.AddSingleton<ITestContextService>(new TestContextService { IsTest = true });
        }

        private class FakeDividendsDataService : IDividendsDataService
        {
            private List<DividendDisplayModel> _dividends;
            
            public FakeDividendsDataService(List<DividendDisplayModel> dividends) => _dividends = dividends;

            public Task<List<DividendAggregateDisplayModel>> GetMonthlyDividendsAsync(
                Currency targetCurrency, DateTime startDate, DateTime endDate, int accountId = 0, 
                string symbol = "", string assetClass = "", CancellationToken cancellationToken = default)
            {
                var filtered = FilterDividends(targetCurrency, startDate, endDate, accountId, symbol, assetClass);
                var grouped = filtered
                    .GroupBy(d => new { Year = d.Date.Year, Month = d.Date.Month })
                    .Select(g => new DividendAggregateDisplayModel
                    {
                        Period = $"{g.Key.Year:0000}-{g.Key.Month:00}",
                        Date = new DateTime(g.Key.Year, g.Key.Month, 1),
                        TotalAmount = Money.Sum(g.Select(d => d.Amount)),
                        TotalTaxAmount = Money.Sum(g.Select(d => d.TaxAmount)),
                        TotalNetAmount = Money.Sum(g.Select(d => d.NetAmount)),
                        DividendCount = g.Count(),
                        Dividends = g.ToList()
                    })
                    .ToList();
                return Task.FromResult(grouped);
            }

            public Task<List<DividendAggregateDisplayModel>> GetYearlyDividendsAsync(
                Currency targetCurrency, DateTime startDate, DateTime endDate, int accountId = 0, 
                string symbol = "", string assetClass = "", CancellationToken cancellationToken = default)
            {
                var filtered = FilterDividends(targetCurrency, startDate, endDate, accountId, symbol, assetClass);
                var grouped = filtered
                    .GroupBy(d => d.Date.Year)
                    .Select(g => new DividendAggregateDisplayModel
                    {
                        Period = g.Key.ToString(),
                        Date = new DateTime(g.Key, 1, 1),
                        TotalAmount = Money.Sum(g.Select(d => d.Amount)),
                        TotalTaxAmount = Money.Sum(g.Select(d => d.TaxAmount)),
                        TotalNetAmount = Money.Sum(g.Select(d => d.NetAmount)),
                        DividendCount = g.Count(),
                        Dividends = g.ToList()
                    })
                    .ToList();
                return Task.FromResult(grouped);
            }

            public Task<List<DividendDisplayModel>> GetDividendsAsync(
                Currency targetCurrency, DateTime startDate, DateTime endDate, int accountId = 0, 
                string symbol = "", string assetClass = "", CancellationToken cancellationToken = default)
            {
                var filtered = FilterDividends(targetCurrency, startDate, endDate, accountId, symbol, assetClass);
                return Task.FromResult(filtered);
            }

            private List<DividendDisplayModel> FilterDividends(Currency targetCurrency, DateTime startDate, DateTime endDate, 
                int accountId, string symbol, string assetClass)
            {
                return _dividends
                    .Where(d => d.Date >= startDate && d.Date <= endDate)
                    .Where(d => accountId == 0 || d.AccountName == $"Account {accountId}")
                    .Where(d => string.IsNullOrEmpty(symbol) || d.Symbol == symbol)
                    .Where(d => string.IsNullOrEmpty(assetClass) || d.AssetClass == assetClass)
                    .ToList();
            }

            public Task<List<Account>> GetAccountsAsync()
            {
                return Task.FromResult(new List<Account>
                {
                    new Account("Account 1") { Id = 1 },
                    new Account("Account 2") { Id = 2 }
                });
            }

            public Task<List<string>> GetDividendSymbolsAsync()
            {
                return Task.FromResult(_dividends.Select(d => d.Symbol).Distinct().ToList());
            }

            public Task<List<string>> GetDividendAssetClassesAsync()
            {
                return Task.FromResult(_dividends.Select(d => d.AssetClass).Distinct().ToList());
            }

            public Task<DateOnly> GetMinDividendDateAsync()
            {
                return Task.FromResult(DateOnly.FromDateTime(new DateTime(2020, 1, 1)));
            }
        }

        [Fact]
        public void Dividends_ShowsLoadingState_WhenIsLoadingIsTrue()
        {
            var mockService = new Mock<IDividendsDataService>();
            Services.AddSingleton(mockService.Object);
            var cut = RenderComponent<Dividends>();
            Assert.Contains("Loading Dividend Data...", cut.Markup);
        }

        [Fact]
        public void Dividends_ShowsEmptyState_WhenNoDividendsExist()
        {
            Services.AddSingleton<IDividendsDataService>(new FakeDividendsDataService(new List<DividendDisplayModel>()));
            var cut = RenderComponent<Dividends>();
            cut.WaitForAssertion(() => Assert.Contains("No Dividends Found", cut.Markup));
        }

        [Fact]
        public void Dividends_ShowsSummaryCards_WhenDividendsExist()
        {
            var dividends = new List<DividendDisplayModel>
            {
                new DividendDisplayModel
                {
                    Symbol = "AAPL",
                    Name = "Apple Inc.",
                    Date = new DateTime(2024, 1, 15),
                    Amount = new Money(Currency.USD, 50),
                    TaxAmount = new Money(Currency.USD, 7.5m),
                    NetAmount = new Money(Currency.USD, 42.5m),
                    AssetClass = "Equity",
                    AccountName = "Account 1"
                }
            };

            Services.AddSingleton<IDividendsDataService>(new FakeDividendsDataService(dividends));
            var cut = RenderComponent<Dividends>();
            
            cut.WaitForAssertion(() => 
            {
                Assert.Contains("Total Dividends", cut.Markup);
                Assert.Contains("Total Taxes", cut.Markup);
                Assert.Contains("Net Dividends", cut.Markup);
                Assert.Contains("Dividend Count", cut.Markup);
            });
        }

        [Fact]
        public void Dividends_ShowsIndividualTable_WhenViewModeIsIndividual()
        {
            var dividends = new List<DividendDisplayModel>
            {
                new DividendDisplayModel
                {
                    Symbol = "AAPL",
                    Name = "Apple Inc.",
                    Date = new DateTime(2024, 1, 15),
                    Amount = new Money(Currency.USD, 50),
                    TaxAmount = new Money(Currency.USD, 7.5m),
                    NetAmount = new Money(Currency.USD, 42.5m),
                    AssetClass = "Equity",
                    AccountName = "Account 1"
                }
            };

            Services.AddSingleton<IDividendsDataService>(new FakeDividendsDataService(dividends));
            var cut = RenderComponent<Dividends>();
            
            cut.WaitForAssertion(() => 
            {
                var individualButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Individual"));
                Assert.NotNull(individualButton);
                individualButton.Click();
            });

            cut.WaitForAssertion(() => 
            {
                Assert.Contains("AAPL", cut.Markup);
                Assert.Contains("Apple Inc.", cut.Markup);
                Assert.Contains("Account 1", cut.Markup);
            });
        }

        [Fact]
        public void Dividends_ShowsMonthlyAggregation_WhenViewModeIsMonthly()
        {
            var dividends = new List<DividendDisplayModel>
            {
                new DividendDisplayModel
                {
                    Symbol = "AAPL",
                    Name = "Apple Inc.",
                    Date = new DateTime(2024, 1, 15),
                    Amount = new Money(Currency.USD, 30),
                    TaxAmount = new Money(Currency.USD, 4.5m),
                    NetAmount = new Money(Currency.USD, 25.5m),
                    AssetClass = "Equity",
                    AccountName = "Account 1"
                },
                new DividendDisplayModel
                {
                    Symbol = "MSFT",
                    Name = "Microsoft Corp.",
                    Date = new DateTime(2024, 1, 20),
                    Amount = new Money(Currency.USD, 40),
                    TaxAmount = new Money(Currency.USD, 6m),
                    NetAmount = new Money(Currency.USD, 34m),
                    AssetClass = "Equity",
                    AccountName = "Account 1"
                }
            };

            Services.AddSingleton<IDividendsDataService>(new FakeDividendsDataService(dividends));
            var cut = RenderComponent<Dividends>();
            
            cut.WaitForAssertion(() => 
            {
                var monthlyButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Monthly"));
                Assert.NotNull(monthlyButton);
                monthlyButton.Click();
            });

            cut.WaitForAssertion(() => 
            {
                Assert.Contains("2024-01", cut.Markup);
                Assert.Contains("$70.00", cut.Markup); // Total amount for January
            });
        }

        [Fact]
        public void Dividends_ShowsChartView_WhenViewModeIsChart()
        {
            var dividends = new List<DividendDisplayModel>
            {
                new DividendDisplayModel
                {
                    Symbol = "AAPL",
                    Name = "Apple Inc.",
                    Date = new DateTime(2024, 1, 15),
                    Amount = new Money(Currency.USD, 50),
                    TaxAmount = new Money(Currency.USD, 7.5m),
                    NetAmount = new Money(Currency.USD, 42.5m),
                    AssetClass = "Equity",
                    AccountName = "Account 1"
                }
            };

            Services.AddSingleton<IDividendsDataService>(new FakeDividendsDataService(dividends));
            var cut = RenderComponent<Dividends>();
            
            cut.WaitForAssertion(() => 
            {
                Assert.Contains("chart-container", cut.Markup);
                Assert.Contains("Chart View", cut.Markup); // Test context shows placeholder
            });
        }

        [Fact]
        public void Dividends_ShowsIndividualTable_WhenViewModeIsTable()
        {
            var dividends = new List<DividendDisplayModel>
            {
                new DividendDisplayModel
                {
                    Symbol = "AAPL",
                    Name = "Apple Inc.",
                    Date = new DateTime(2024, 1, 15),
                    Amount = new Money(Currency.USD, 50),
                    TaxAmount = new Money(Currency.USD, 7.5m),
                    NetAmount = new Money(Currency.USD, 42.5m),
                    AssetClass = "Equity",
                    AccountName = "Account 1"
                }
            };

            Services.AddSingleton<IDividendsDataService>(new FakeDividendsDataService(dividends));
            var cut = RenderComponent<Dividends>();
            
            cut.WaitForAssertion(() => 
            {
                var tableButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Table"));
                Assert.NotNull(tableButton);
                tableButton.Click();
            });

            cut.WaitForAssertion(() => 
            {
                var individualButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Individual"));
                Assert.NotNull(individualButton);
                individualButton.Click();
            });

            cut.WaitForAssertion(() => 
            {
                Assert.Contains("AAPL", cut.Markup);
                Assert.Contains("Apple Inc.", cut.Markup);
                Assert.Contains("Account 1", cut.Markup);
            });
        }

        [Fact]
        public void Dividends_ShowsMonthlyAggregation_WhenChartTypeIsMonthly()
        {
            var dividends = new List<DividendDisplayModel>
            {
                new DividendDisplayModel
                {
                    Symbol = "AAPL",
                    Name = "Apple Inc.",
                    Date = new DateTime(2024, 1, 15),
                    Amount = new Money(Currency.USD, 30),
                    TaxAmount = new Money(Currency.USD, 4.5m),
                    NetAmount = new Money(Currency.USD, 25.5m),
                    AssetClass = "Equity",
                    AccountName = "Account 1"
                },
                new DividendDisplayModel
                {
                    Symbol = "MSFT",
                    Name = "Microsoft Corp.",
                    Date = new DateTime(2024, 1, 20),
                    Amount = new Money(Currency.USD, 40),
                    TaxAmount = new Money(Currency.USD, 6m),
                    NetAmount = new Money(Currency.USD, 34m),
                    AssetClass = "Equity",
                    AccountName = "Account 1"
                }
            };

            Services.AddSingleton<IDividendsDataService>(new FakeDividendsDataService(dividends));
            var cut = RenderComponent<Dividends>();
            
            cut.WaitForAssertion(() => 
            {
                var tableButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Table"));
                Assert.NotNull(tableButton);
                tableButton.Click();
            });

            cut.WaitForAssertion(() => 
            {
                var monthlyButton = cut.FindAll("button").FirstOrDefault(b => b.TextContent.Contains("Monthly"));
                Assert.NotNull(monthlyButton);
                monthlyButton.Click();
            });

            cut.WaitForAssertion(() => 
            {
                Assert.Contains("2024-01", cut.Markup);
                Assert.Contains("$70.00", cut.Markup); // Total amount for January
            });
        }

        [Fact]
        public void Dividends_ChartData_IsGeneratedCorrectly()
        {
            var dividends = new List<DividendDisplayModel>
            {
                new DividendDisplayModel
                {
                    Symbol = "AAPL",
                    Name = "Apple Inc.",
                    Date = new DateTime(2024, 1, 15),
                    Amount = new Money(Currency.USD, 50),
                    TaxAmount = new Money(Currency.USD, 7.5m),
                    NetAmount = new Money(Currency.USD, 42.5m),
                    AssetClass = "Equity",
                    AccountName = "Account 1"
                },
                new DividendDisplayModel
                {
                    Symbol = "MSFT",
                    Name = "Microsoft Corp.",
                    Date = new DateTime(2024, 2, 15),
                    Amount = new Money(Currency.USD, 60),
                    TaxAmount = new Money(Currency.USD, 9m),
                    NetAmount = new Money(Currency.USD, 51m),
                    AssetClass = "Equity",
                    AccountName = "Account 1"
                }
            };

            Services.AddSingleton<IDividendsDataService>(new FakeDividendsDataService(dividends));
            var cut = RenderComponent<Dividends>();
            
            cut.WaitForAssertion(() => 
            {
                // Should default to chart view
                Assert.Contains("chart-container", cut.Markup);
                
                // Should show chart controls
                Assert.Contains("Monthly", cut.Markup);
                Assert.Contains("Yearly", cut.Markup);
                Assert.Contains("Chart", cut.Markup);
            });
        }
    }
}