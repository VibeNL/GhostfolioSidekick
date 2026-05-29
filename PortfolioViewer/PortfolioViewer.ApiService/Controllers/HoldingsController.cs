using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GhostfolioSidekick.PortfolioViewer.ApiService.Controllers
{
	[ApiController]
	[Route("api/[controller]")]
	public class HoldingsController(
		IDbContextFactory<DatabaseContext> dbContextFactory,
		IApplicationSettings applicationSettings) : ControllerBase
	{
		private string PrimaryCurrencySymbol =>
			applicationSettings.ConfigurationInstance?.Settings?.PrimaryCurrency ?? "EUR";

		[HttpGet]
		public async Task<IActionResult> GetHoldings(CancellationToken cancellationToken)
		{
			var result = await GetHoldingsInternalAsync(null, cancellationToken);
			return Ok(result);
		}

		[HttpGet("account/{accountId:int}")]
		public async Task<IActionResult> GetHoldingsByAccount(int accountId, CancellationToken cancellationToken)
		{
			var result = await GetHoldingsInternalAsync(accountId == 0 ? null : accountId, cancellationToken);
			return Ok(result);
		}

		[HttpGet("{symbol}")]
		public async Task<IActionResult> GetHolding(string symbol, CancellationToken cancellationToken)
		{
			var all = await GetHoldingsInternalAsync(null, cancellationToken);
			var holding = all.FirstOrDefault(x => x.Symbols.Contains(symbol));
			if (holding == null)
			{
				return NotFound();
			}

			return Ok(holding);
		}

		[HttpGet("{symbol}/price-history")]
		public async Task<IActionResult> GetHoldingPriceHistory(
			string symbol,
			[FromQuery] DateOnly startDate,
			[FromQuery] DateOnly endDate,
			CancellationToken cancellationToken)
		{
			using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
			var rawSnapShots = await db.Holdings
				.Where(x => x.SymbolProfiles.Any(sp => sp.Symbol == symbol))
				.SelectMany(x => x.CalculatedSnapshots)
				.Where(x => x.Date >= startDate && x.Date <= endDate)
				.GroupBy(x => x.Date)
				.AsNoTracking()
				.ToListAsync(cancellationToken);

			var snapShots = new List<HoldingPriceHistoryPointDto>();
			foreach (var group in rawSnapShots)
			{
				var totalQuantity = group.Sum(x => x.Quantity);
				snapShots.Add(new HoldingPriceHistoryPointDto
				{
					Date = group.Key,
					Price = group.Min(x => x.CurrentUnitPrice),
					AveragePrice = totalQuantity == 0 ? 0 : group.Sum(y => y.AverageCostPrice * y.Quantity) / totalQuantity,
				});
			}

			return Ok(snapShots);
		}

		[HttpGet("portfolio-value-history")]
		public async Task<IActionResult> GetPortfolioValueHistory(
			[FromQuery] DateOnly startDate,
			[FromQuery] DateOnly endDate,
			[FromQuery] int? accountId,
			CancellationToken cancellationToken)
		{
			using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);

			var snapshotPoints = await db.CalculatedSnapshots
				.Where(x => (accountId == null || accountId == 0 || x.AccountId == accountId) &&
							x.Date >= startDate &&
							x.Date <= endDate)
				.Where(x => x.Holding != null && x.Holding.SymbolProfiles.Any())
				.GroupBy(x => x.Date)
				.Select(g => new { Date = g.Key, Value = g.Sum(x => x.TotalValue), Invested = g.Sum(x => x.TotalInvested) })
				.AsNoTracking()
				.ToListAsync(cancellationToken);

			var primaryCurrency = PrimaryCurrencySymbol;
			var balanceRecords = await db.Balances
				.AsNoTracking()
				.Where(b => (accountId == null || accountId == 0 || b.AccountId == accountId) && b.Date <= endDate && b.Money.Currency.Symbol == primaryCurrency)
				.Select(b => new { b.Date, b.AccountId, Amount = b.Money.Amount })
				.ToListAsync(cancellationToken);

			var balancesByAccount = balanceRecords
				.GroupBy(b => b.AccountId)
				.ToDictionary(g => g.Key, g => g.OrderBy(b => b.Date).ToList());

			var balanceDatesInRange = balanceRecords
				.Where(b => b.Date >= startDate)
				.Select(b => b.Date);

			var allDates = snapshotPoints
				.Select(s => s.Date)
				.Union(balanceDatesInRange)
				.OrderBy(d => d)
				.ToList();

			var snapshotByDate = snapshotPoints.ToDictionary(s => s.Date);

			var result = new List<PortfolioValueHistoryPointDto>(allDates.Count);
			decimal lastValue = 0;
			decimal lastInvested = 0;

			foreach (var date in allDates)
			{
				if (snapshotByDate.TryGetValue(date, out var snap))
				{
					lastValue = snap.Value;
					lastInvested = snap.Invested;
				}

				decimal totalBalance = 0;
				foreach (var (_, balances) in balancesByAccount)
				{
					var latestBalance = balances.LastOrDefault(b => b.Date <= date);
					if (latestBalance != null)
					{
						totalBalance += latestBalance.Amount;
					}
				}

				result.Add(new PortfolioValueHistoryPointDto
				{
					Date = date,
					Value = lastValue,
					Invested = lastInvested,
					Balance = totalBalance
				});
			}

			return Ok(result);
		}

		private async Task<List<HoldingDisplayModelDto>> GetHoldingsInternalAsync(int? accountId, CancellationToken cancellationToken)
		{
			using var db = await dbContextFactory.CreateDbContextAsync(cancellationToken);
			var lastKnownDate = await db.CalculatedSnapshots
				.Where(x => accountId == null || x.AccountId == accountId)
				.MaxAsync(x => (DateOnly?)x.Date, cancellationToken);

			var list = await db.Holdings
				.Where(x => x.CalculatedSnapshots.Any(y => y.AccountId == accountId || accountId == null))
				.Select(x => new
				{
					Holding = x,
					Snapshots = x.CalculatedSnapshots
						.Where(s => s.AccountId == accountId || accountId == null)
						.Where(s => s.Date == lastKnownDate)
				})
				.OrderBy(x => x.Holding.Id)
				.ToListAsync(cancellationToken);

			var primaryCurrency = PrimaryCurrencySymbol;
			var result = new List<HoldingDisplayModelDto>();

			foreach (var x in list)
			{
				if (!x.Snapshots.Any() || x.Holding.SymbolProfiles.Count == 0)
				{
					continue;
				}

				var symbolProfile = x.Holding.SymbolProfiles.OrderBy(sp => Datasource.GetPriority(sp.DataSource)).First();
				var quantity = x.Snapshots.Sum(y => y.Quantity);
				var averagePrice = SafeDivide(x.Snapshots.Sum(y => y.AverageCostPrice * y.Quantity), quantity);
				var currentValue = x.Snapshots.Sum(y => y.TotalValue);

				result.Add(new HoldingDisplayModelDto
				{
					AssetClass = symbolProfile.AssetClass.ToString(),
					AveragePrice = averagePrice,
					Currency = primaryCurrency,
					CurrentPrice = x.Snapshots.Min(y => y.CurrentUnitPrice),
					CurrentValue = currentValue,
					Name = symbolProfile.Name ?? symbolProfile.Symbol,
					Quantity = quantity,
					Sector = symbolProfile.SectorWeights.Select(sw => sw.Name).FirstOrDefault() ?? string.Empty,
					Symbols = x.Holding.SymbolProfiles.Select(sp => sp.Symbol).Distinct().ToList(),
					GainLoss = currentValue - (averagePrice * quantity),
					GainLossPercentage = averagePrice * quantity == 0 ? 0 : (currentValue - (averagePrice * quantity)) / (averagePrice * quantity),
				});
			}

			var totalValue = result.Sum(x => x.CurrentValue);
			if (totalValue > 0)
			{
				foreach (var x in result)
				{
					x.Weight = x.CurrentValue / totalValue;
				}
			}

			return [.. result.OrderBy(x => x.Symbols.FirstOrDefault())];
		}

		private static decimal SafeDivide(decimal a, decimal b)
		{
			if (b == 0) { return 0; }
			return a / b;
		}

		public class HoldingDisplayModelDto
		{
			public List<string> Symbols { get; set; } = [];
			public string Name { get; set; } = string.Empty;
			public decimal CurrentValue { get; set; }
			public decimal Quantity { get; set; }
			public decimal AveragePrice { get; set; }
			public decimal CurrentPrice { get; set; }
			public decimal GainLoss { get; set; }
			public decimal GainLossPercentage { get; set; }
			public decimal Weight { get; set; }
			public string Sector { get; set; } = string.Empty;
			public string AssetClass { get; set; } = string.Empty;
			public string Currency { get; set; } = "EUR";
		}

		public class HoldingPriceHistoryPointDto
		{
			public DateOnly Date { get; set; }
			public decimal Price { get; set; }
			public decimal AveragePrice { get; set; }
		}

		public class PortfolioValueHistoryPointDto
		{
			public DateOnly Date { get; set; }
			public decimal Value { get; set; }
			public decimal Invested { get; set; }
			public decimal Balance { get; set; }
		}
	}
}
