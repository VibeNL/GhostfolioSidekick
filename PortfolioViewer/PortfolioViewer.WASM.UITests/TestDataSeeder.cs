using GhostfolioSidekick.Database;
using Microsoft.EntityFrameworkCore;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Performance;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.Model.Tasks;

namespace PortfolioViewer.WASM.UITests;

/// <summary>
/// Seeds comprehensive test data across all 15 database tables for UI tests.
/// </summary>
public static class TestDataSeeder
{
	/// <summary>
	/// Seeds all tables with realistic test data. Call after EnsureCreated().
	/// </summary>
	public static void Seed(DatabaseContext db)
	{
		// Skip if data already exists
		var accountCount = db.Accounts.Count();
		var symbolCount = db.SymbolProfiles.Count();
		if (accountCount > 0 || symbolCount > 0)
			return;

		// Keep seeded data anchored to "now" so date-range based pages are populated over time.
		DateTime baseDate = DateTime.UtcNow.Date;

		// 1. Platforms
		var platform = new Platform("Test Platform");
		db.Platforms.Add(platform);

		// 2. Accounts
		var account = new Account("Test Account");
		db.Accounts.Add(account);
		db.SaveChanges(); // Save to get account.Id

		// 3. Balances
		var balances = new List<Balance>
		{
			new(DateOnly.FromDateTime(baseDate.AddDays(-60)), new Money(Currency.USD, 50000m)),
			new(DateOnly.FromDateTime(baseDate.AddDays(-30)), new Money(Currency.USD, 55000m)),
			new(DateOnly.FromDateTime(baseDate), new Money(Currency.USD, 62000m))
		};
		foreach (var b in balances)
		{
			b.AccountId = account.Id;
			db.Balances.Add(b);
		}
		db.SaveChanges();

		// 4. SymbolProfiles
		var aaplProfile = new SymbolProfile("AAPL", "Apple Inc.", [], Currency.USD, "NASDAQ", AssetClass.Equity, null, [], []);
		var googlProfile = new SymbolProfile("GOOGL", "Alphabet Inc.", [], Currency.USD, "NASDAQ", AssetClass.Equity, null, [], []);
		var btcProfile = new SymbolProfile("BTC", "Bitcoin", [], Currency.USD, "Crypto", AssetClass.Commodity, AssetSubClass.CryptoCurrency, [], []);
		var etfProfile = new SymbolProfile("VTI", "Vanguard Total Stock Market ETF", [], Currency.USD, "NYSE", AssetClass.Equity, AssetSubClass.Etf, [], []);
		var bondProfile = new SymbolProfile("US10Y", "US 10-Year Treasury", [], Currency.USD, "USGovt", AssetClass.FixedIncome, AssetSubClass.Bond, [], []);
		var cashProfile = new SymbolProfile("CASH", "Cash", [], Currency.USD, "Cash", AssetClass.Liquidity, null, [], []);

		var symbolProfiles = new List<SymbolProfile> { aaplProfile, googlProfile, btcProfile, etfProfile, bondProfile, cashProfile };
		db.SymbolProfiles.AddRange(symbolProfiles);
		db.SaveChanges(); // Save to get SymbolProfile IDs for MarketData FK

		// 5. Holdings
		var holding1 = new Holding { SymbolProfiles = [aaplProfile] };
		var holding2 = new Holding { SymbolProfiles = [googlProfile] };
		var holding3 = new Holding { SymbolProfiles = [btcProfile] };
		var holding4 = new Holding { SymbolProfiles = [etfProfile] };
		var holding5 = new Holding { SymbolProfiles = [bondProfile] };
		var holdings = new List<Holding> { holding1, holding2, holding3, holding4, holding5 };
		db.Holdings.AddRange(holdings);
		db.SaveChanges(); // Save to get Holding IDs

		// 6. PartialSymbolIdentifiers
		var identifiers = new List<PartialSymbolIdentifier>
		{
			PartialSymbolIdentifier.CreateStockAndETF(IdentifierType.Default, "AAPL", Currency.USD)!,
			PartialSymbolIdentifier.CreateStockAndETF(IdentifierType.Default, "GOOGL", Currency.USD)!,
			PartialSymbolIdentifier.CreateCrypto(IdentifierType.Default, "BTC", Currency.USD)!,
			PartialSymbolIdentifier.CreateStockAndETF(IdentifierType.Default, "VTI", Currency.USD)!,
			PartialSymbolIdentifier.CreateStockBondAndETF(IdentifierType.Default, "US10Y", Currency.USD)!
		};
		foreach (var h in holdings)
		{
			h.PartialSymbolIdentifiers.Add(identifiers[holdings.IndexOf(h)]);
		}

		// 7. Activities
		var activities = new List<Activity>
		{
			// Cash deposit
			new CashDepositActivity(
				account, null, baseDate.AddDays(-30), new Money(Currency.USD, 50000m),
				"CASH-DEP-001", null, "Initial cash deposit"),

			// AAPL buys
			new BuyActivity(account, holding1, [], baseDate.AddDays(-28), 20m, new Money(Currency.USD, 3500m), new Money(Currency.USD, 3500m).Times(20), "BUY-AAPL-001", null, "Buy Apple shares") { PartialSymbolIdentifiers = [identifiers[0]] },
			new BuyActivity(account, holding1, [], baseDate.AddDays(-14), 10m, new Money(Currency.USD, 3800m), new Money(Currency.USD, 3800m).Times(10), "BUY-AAPL-002", null, "Buy more Apple") { PartialSymbolIdentifiers = [identifiers[0]] },

			// GOOGL buy
			new BuyActivity(account, holding2, [], baseDate.AddDays(-20), 5m, new Money(Currency.USD, 2000m), new Money(Currency.USD, 2000m).Times(5), "BUY-GOOGL-001", null, "Buy Alphabet") { PartialSymbolIdentifiers = [identifiers[1]] },

			// BTC buy
			new BuyActivity(account, holding3, [], baseDate.AddDays(-25), 0.5m, new Money(Currency.USD, 25000m), new Money(Currency.USD, 25000m).Times(0.5m), "BUY-BTC-001", null, "Buy Bitcoin") { PartialSymbolIdentifiers = [identifiers[2]] },

			// VTI buy
			new BuyActivity(account, holding4, [], baseDate.AddDays(-10), 30m, new Money(Currency.USD, 5700m), new Money(Currency.USD, 5700m).Times(30), "BUY-VTI-001", null, "Buy VTI ETF") { PartialSymbolIdentifiers = [identifiers[3]] },

			// Bond buy
			new BuyActivity(account, holding5, [], baseDate.AddDays(-5), 10m, new Money(Currency.USD, 980m), new Money(Currency.USD, 980m).Times(10), "BUY-BOND-001", null, "Buy US 10Y Bond") { PartialSymbolIdentifiers = [identifiers[4]] },

			// AAPL dividend
			new DividendActivity(account, holding1, [], baseDate.AddDays(-2), new Money(Currency.USD, 50m), "DIV-AAPL-001", null, "Apple Q4 dividend") { PartialSymbolIdentifiers = [identifiers[0]] },

			// VTI dividend
			new DividendActivity(account, holding4, [], baseDate.AddDays(-1), new Money(Currency.USD, 75m), "DIV-VTI-001", null, "VTI quarterly dividend") { PartialSymbolIdentifiers = [identifiers[3]] },

			// Cash deposit
			new CashDepositActivity(account, null, baseDate.AddDays(-1), new Money(Currency.USD, 5000m), "CASH-DEP-002", null, "Additional cash"),

			// Sell some AAPL
			new SellActivity(account, holding1, [], baseDate.AddDays(0), 5m, new Money(Currency.USD, 1900m), new Money(Currency.USD, 1900m).Times(5), "SELL-AAPL-001", null, "Sell some Apple") { PartialSymbolIdentifiers = [identifiers[0]] }
		};

		db.Activities.AddRange(activities);
		db.SaveChanges(); // Save activities before snapshots

		// 8. CurrencyExchangeProfile + CurrencyExchangeRate
		var currencyProfile = new CurrencyExchangeProfile(Currency.USD, Currency.EUR)
		{
			Rates =
			[
				new CurrencyExchangeRate(Currency.USD, 0.92m, 0.93m, 0.94m, 0.91m, 1000000m, DateOnly.FromDateTime(baseDate.AddDays(-10))),
				new CurrencyExchangeRate(Currency.USD, 0.91m, 0.92m, 0.93m, 0.90m, 1100000m, DateOnly.FromDateTime(baseDate.AddDays(-5))),
				new CurrencyExchangeRate(Currency.USD, 0.93m, 0.92m, 0.94m, 0.91m, 1050000m, DateOnly.FromDateTime(baseDate.AddDays(0)))
			]
		};
		db.CurrencyExchangeRates.Add(currencyProfile);

		// 9. MarketData (shadow FK properties set via EF Core)
		var marketDataList = new List<MarketData>
		{
			new MarketData(Currency.USD, 185m, 183m, 188m, 182m, 50000000m, DateOnly.FromDateTime(baseDate.AddDays(-30))),
			new MarketData(Currency.USD, 190m, 186m, 192m, 184m, 55000000m, DateOnly.FromDateTime(baseDate.AddDays(-20))),
			new MarketData(Currency.USD, 188m, 189m, 191m, 186m, 48000000m, DateOnly.FromDateTime(baseDate.AddDays(-10))),
			new MarketData(Currency.USD, 195m, 188m, 196m, 187m, 60000000m, DateOnly.FromDateTime(baseDate.AddDays(-5))),
			new MarketData(Currency.USD, 198m, 194m, 200m, 193m, 52000000m, DateOnly.FromDateTime(baseDate.AddDays(0))),
			new MarketData(Currency.USD, 140m, 138m, 142m, 137m, 30000000m, DateOnly.FromDateTime(baseDate.AddDays(-30))),
			new MarketData(Currency.USD, 145m, 141m, 147m, 139m, 32000000m, DateOnly.FromDateTime(baseDate.AddDays(0))),
			new MarketData(Currency.USD, 60000m, 59000m, 61000m, 58500m, 100000000m, DateOnly.FromDateTime(baseDate.AddDays(-30))),
			new MarketData(Currency.USD, 65000m, 60000m, 66000m, 59000m, 110000000m, DateOnly.FromDateTime(baseDate.AddDays(0))),
			new MarketData(Currency.USD, 190m, 188m, 192m, 187m, 20000000m, DateOnly.FromDateTime(baseDate.AddDays(-30))),
			new MarketData(Currency.USD, 195m, 190m, 197m, 189m, 22000000m, DateOnly.FromDateTime(baseDate.AddDays(0)))
		};
		db.MarketDatas.AddRange(marketDataList);

		// Set required shadow properties for MarketData FK (unique composite: DataSource, Symbol, Date)
		for (int i = 0; i < marketDataList.Count; i++)
		{
			var md = marketDataList[i];
			// Each entry needs a unique (DataSource, Symbol, Date) combination
			var symbolData = new (string Symbol, string DataSource)[]
			{
				("AAPL", "NASDAQ"),   // i=0: AAPL -30 days
				("AAPL", "NASDAQ"),   // i=1: AAPL -20 days (different date)
				("AAPL", "NASDAQ"),   // i=2: AAPL -10 days (different date)
				("AAPL", "NASDAQ"),   // i=3: AAPL -5 days (different date)
				("AAPL", "NASDAQ"),   // i=4: AAPL today (different date)
				("GOOGL", "NASDAQ"),  // i=5: GOOGL -30 days
				("GOOGL", "NASDAQ"),  // i=6: GOOGL today (different date)
				("BTC", "Crypto"),    // i=7: BTC -30 days
				("BTC", "Crypto"),    // i=8: BTC today (different date)
				("VTI", "NYSE"),      // i=9: VTI -30 days
				("VTI", "NYSE"),      // i=10: VTI today (different date)
			};
			db.Entry(md).Property("SymbolProfileSymbol").CurrentValue = symbolData[i].Symbol;
			db.Entry(md).Property("SymbolProfileDataSource").CurrentValue = symbolData[i].DataSource;
		}
		db.MarketDatas.AddRange(marketDataList);

		// 10. CalculatedSnapshots
		var snapshots = new List<CalculatedSnapshot>
		{
			new(Guid.NewGuid(), account.Id, DateOnly.FromDateTime(baseDate.AddDays(-28)), 20m, Currency.USD, 175m, 170m, 3500m, 3400m) { HoldingId = holding1.Id },
			new(Guid.NewGuid(), account.Id, DateOnly.FromDateTime(baseDate), 25m, Currency.USD, 180m, 198m, 4500m, 4950m) { HoldingId = holding1.Id },

			new(Guid.NewGuid(), account.Id, DateOnly.FromDateTime(baseDate.AddDays(-20)), 5m, Currency.USD, 400m, 140m, 2000m, 700m) { HoldingId = holding2.Id },
			new(Guid.NewGuid(), account.Id, DateOnly.FromDateTime(baseDate), 5m, Currency.USD, 400m, 145m, 2000m, 725m) { HoldingId = holding2.Id },

			new(Guid.NewGuid(), account.Id, DateOnly.FromDateTime(baseDate.AddDays(-25)), 0.5m, Currency.USD, 50000m, 60000m, 25000m, 30000m) { HoldingId = holding3.Id },
			new(Guid.NewGuid(), account.Id, DateOnly.FromDateTime(baseDate), 0.5m, Currency.USD, 50000m, 65000m, 25000m, 32500m) { HoldingId = holding3.Id },

			new(Guid.NewGuid(), account.Id, DateOnly.FromDateTime(baseDate.AddDays(-10)), 30m, Currency.USD, 190m, 190m, 5700m, 5700m) { HoldingId = holding4.Id },
			new(Guid.NewGuid(), account.Id, DateOnly.FromDateTime(baseDate), 30m, Currency.USD, 190m, 195m, 5700m, 5850m) { HoldingId = holding4.Id },

			new(Guid.NewGuid(), account.Id, DateOnly.FromDateTime(baseDate.AddDays(-5)), 10m, Currency.USD, 98m, 99m, 980m, 990m) { HoldingId = holding5.Id },
			new(Guid.NewGuid(), account.Id, DateOnly.FromDateTime(baseDate), 10m, Currency.USD, 98m, 97m, 980m, 970m) { HoldingId = holding5.Id }
		};
		db.CalculatedSnapshots.AddRange(snapshots);
		db.SaveChanges(); // Save snapshots before dividends

		// 11. Dividends
		var dividends = new List<Dividend>
		{
			new() { Id = 1, ExDividendDate = DateOnly.FromDateTime(baseDate.AddDays(-5)), PaymentDate = DateOnly.FromDateTime(baseDate.AddDays(5)), DividendType = GhostfolioSidekick.Model.Market.DividendType.Cash, DividendState = GhostfolioSidekick.Model.Market.DividendState.Paid, Amount = new Money(Currency.USD, 50m), SymbolProfileSymbol = "AAPL", SymbolProfileDataSource = "NASDAQ" },
			new() { Id = 2, ExDividendDate = DateOnly.FromDateTime(baseDate.AddDays(-1)), PaymentDate = DateOnly.FromDateTime(baseDate.AddDays(10)), DividendType = GhostfolioSidekick.Model.Market.DividendType.Cash, DividendState = GhostfolioSidekick.Model.Market.DividendState.Declared, Amount = new Money(Currency.USD, 75m), SymbolProfileSymbol = "VTI", SymbolProfileDataSource = "NYSE" }
		};
		db.Dividends.AddRange(dividends);

		// 12. UpcomingDividendTimelineEntries
		var upcomingDividends = new List<UpcomingDividendTimelineEntry>
		{
			new() { Id = Guid.NewGuid(), HoldingId = holding4.Id, ExpectedDate = DateOnly.FromDateTime(baseDate.AddDays(10)), ExDate = DateOnly.FromDateTime(baseDate.AddDays(-1)), Amount = 75m, Currency = Currency.USD, AmountPrimaryCurrency = 68m, DividendType = GhostfolioSidekick.Model.Market.DividendType.Cash, DividendState = GhostfolioSidekick.Model.Market.DividendState.Declared },
			new() { Id = Guid.NewGuid(), HoldingId = holding1.Id, ExpectedDate = DateOnly.FromDateTime(baseDate.AddDays(45)), ExDate = DateOnly.FromDateTime(baseDate.AddDays(30)), Amount = 50m, Currency = Currency.USD, AmountPrimaryCurrency = 46m, DividendType = GhostfolioSidekick.Model.Market.DividendType.Cash, DividendState = GhostfolioSidekick.Model.Market.DividendState.Predicted }
		};
		db.UpcomingDividendTimelineEntries.AddRange(upcomingDividends);

		// 13. TaskRun
		var taskRun = new TaskRun
		{
			Type = "Sync",
			Name = "Full Sync",
			LastUpdate = DateTimeOffset.UtcNow,
			Scheduled = true,
			Priority = 1,
			NextSchedule = DateTimeOffset.UtcNow.AddDays(1),
			InProgress = false,
			StartTime = DateTimeOffset.UtcNow.AddDays(-1),
			EndTime = DateTimeOffset.UtcNow.AddMinutes(-30)
		};
		db.Tasks.Add(taskRun);

		// 14. TaskRunLog (FK references TaskRun.Type as principal key)
		var taskLogs = new List<TaskRunLog>
		{
			new() { TaskRun = taskRun, TaskRunType = "Sync", Timestamp = DateTimeOffset.UtcNow.AddDays(-1), Message = "Sync started", Severity = 0 },
			new() { TaskRun = taskRun, TaskRunType = "Sync", Timestamp = DateTimeOffset.UtcNow.AddMinutes(-30), Message = "Sync completed successfully", Severity = 0 },
			new() { TaskRun = taskRun, TaskRunType = "Sync", Timestamp = DateTimeOffset.UtcNow.AddMinutes(-15), Message = "Warning: Some prices could not be fetched", Severity = 1 }
		};
		db.TaskRunLogs.AddRange(taskLogs);


		// 15. ExternalDataCacheEntry
		var cacheEntries = new List<GhostfolioSidekick.Database.Cache.ExternalDataCacheEntry>
		{
			new() { Key = "yahoo:AAPL:USD", DataJson = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new { price = 198m, date = baseDate.AddDays(0) }), CreatedAt = baseDate.AddDays(-1), ExpiresAt = baseDate.AddDays(1) },
			new() { Key = "coingecko:bitcoin:usd", DataJson = System.Text.Json.JsonSerializer.SerializeToUtf8Bytes(new { price = 65000m, date = baseDate.AddDays(0) }), CreatedAt = baseDate.AddDays(-1), ExpiresAt = baseDate.AddDays(1) }
		};
		db.ExternalDataCacheEntries.AddRange(cacheEntries);

		// 16. PriceTargets (analyst price targets)
		var priceTargets = new List<PriceTarget>
		{
			new() { Symbol = "AAPL", HighestTargetPriceAmount = 250m, HighestTargetCurrency = Currency.USD, AverageTargetPriceAmount = 210m, AverageTargetCurrency = Currency.USD, LowestTargetPriceAmount = 175m, LowestTargetCurrency = Currency.USD, Rating = AnalystRating.Buy, NumberOfBuys = 15, NumberOfHolds = 8, NumberOfSells = 2 },
			new() { Symbol = "GOOGL", HighestTargetPriceAmount = 220m, HighestTargetCurrency = Currency.USD, AverageTargetPriceAmount = 185m, AverageTargetCurrency = Currency.USD, LowestTargetPriceAmount = 140m, LowestTargetCurrency = Currency.USD, Rating = AnalystRating.Hold, NumberOfBuys = 10, NumberOfHolds = 12, NumberOfSells = 5 },
			new() { Symbol = "BTC", HighestTargetPriceAmount = 120000m, HighestTargetCurrency = Currency.USD, AverageTargetPriceAmount = 85000m, AverageTargetCurrency = Currency.USD, LowestTargetPriceAmount = 50000m, LowestTargetCurrency = Currency.USD, Rating = AnalystRating.StrongBuy, NumberOfBuys = 20, NumberOfHolds = 3, NumberOfSells = 0 },
			new() { Symbol = "VTI", HighestTargetPriceAmount = 320m, HighestTargetCurrency = Currency.USD, AverageTargetPriceAmount = 270m, AverageTargetCurrency = Currency.USD, LowestTargetPriceAmount = 230m, LowestTargetCurrency = Currency.USD, Rating = AnalystRating.Buy, NumberOfBuys = 8, NumberOfHolds = 5, NumberOfSells = 1 }
		};
		db.PriceTargets.AddRange(priceTargets);

		var rows = db.SaveChanges();
	}
}
