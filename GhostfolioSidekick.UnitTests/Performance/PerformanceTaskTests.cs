using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Performance;
using GhostfolioSidekick.Model.Symbols;
using GhostfolioSidekick.Performance;
using GhostfolioSidekick.PerformanceCalculations;
using Microsoft.Data.Sqlite;

namespace GhostfolioSidekick.UnitTests.Performance
{
	public class PerformanceTaskTests
	{
		private static DbContextOptions<DatabaseContext> CreateOptions(SqliteConnection connection) =>
			new DbContextOptionsBuilder<DatabaseContext>()
				.UseSqlite(connection)
				.Options;

		[Fact]
		public async Task DoWork_CalculatesPerformanceForAllHoldings()
		{
			using var connection = new SqliteConnection("Filename=:memory:");
			await connection.OpenAsync(TestContext.Current.CancellationToken);
			var options = CreateOptions(connection);
			var dbContext = new DatabaseContext(options);
			await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

			var holding = new Holding
			{
				Id = 1,
				SymbolProfiles =
				[
					new SymbolProfile { Symbol = "TEST", Name = "Test Holding", Currency = Currency.USD, DataSource = "YAHOO", AssetClass = AssetClass.Equity, CountryWeight = [], SectorWeights = [], Identifiers = [] }
				]
			};
			dbContext.Holdings.Add(holding);
			await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

			var dbFactoryMock = new Mock<IDbContextFactory<DatabaseContext>>();
			dbFactoryMock.Setup(x => x.CreateDbContextAsync(default)).ReturnsAsync(() => new DatabaseContext(options));

			var newSnapshot = new CalculatedSnapshot
			{
				AccountId = 1,
				Date = new DateOnly(2024, 1, 1),
				Quantity = 10,
				Currency = Currency.USD,
				AverageCostPrice = 100,
				CurrentUnitPrice = 120,
				TotalInvested = 1000,
				TotalValue = 1200
			};

			var calculatorMock = new Mock<IPerformanceCalculator>();
			calculatorMock.Setup(x => x.GetCalculatedSnapshots(It.IsAny<Holding>(), Currency.USD))
				.ReturnsAsync(new List<CalculatedSnapshot> { newSnapshot });

			var loggerMock = new Mock<ILogger>();
			var appSettingsMock = new Mock<IApplicationSettings>();
			var settings = new Settings { PrimaryCurrency = "USD", DataProviderPreference = "YAHOO;COINGECKO" };
			var configInstance = new ConfigurationInstance { Settings = settings };
			appSettingsMock.Setup(x => x.ConfigurationInstance).Returns(configInstance);
			var task = new PerformanceTask(calculatorMock.Object, dbFactoryMock.Object, appSettingsMock.Object);

			// Act
			await task.DoWork(loggerMock.Object);

			// Assert
			var verifyContext = new DatabaseContext(options);
			var updatedHolding = await verifyContext.Holdings
				.Include(h => h.CalculatedSnapshots)
				.FirstAsync(h => h.Id == 1, TestContext.Current.CancellationToken);

			Assert.Single(updatedHolding.CalculatedSnapshots);
			Assert.Equal(10, updatedHolding.CalculatedSnapshots.First().Quantity);
			Assert.Equal(1200, updatedHolding.CalculatedSnapshots.First().TotalValue);

			await dbContext.DisposeAsync();
			await verifyContext.DisposeAsync();
		}

		[Fact]
		public async Task DoWork_UpdatesExistingSnapshots()
		{
			using var connection = new SqliteConnection("Filename=:memory:");
			await connection.OpenAsync(TestContext.Current.CancellationToken);
			var options = CreateOptions(connection);
			var dbContext = new DatabaseContext(options);
			await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

			var holding = new Holding
			{
				Id = 1,
				SymbolProfiles =
				[
					new SymbolProfile { Symbol = "TEST", Name = "Test Holding", Currency = Currency.USD, DataSource = "YAHOO", AssetClass = AssetClass.Equity, CountryWeight = [], SectorWeights = [], Identifiers = [] }
				],
				CalculatedSnapshots =
				[
					new()
					{
						AccountId = 1,
						Date = new DateOnly(2024, 1, 1),
						Quantity = 5,
						Currency = Currency.USD,
						AverageCostPrice = 100,
						CurrentUnitPrice = 110,
						TotalInvested = 500,
						TotalValue = 550,
						HoldingId = 1
					}
				]
			};
			dbContext.Holdings.Add(holding);
			await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

			var dbFactoryMock = new Mock<IDbContextFactory<DatabaseContext>>();
			dbFactoryMock.Setup(x => x.CreateDbContextAsync(default)).ReturnsAsync(() => new DatabaseContext(options));

			var updatedSnapshot = new CalculatedSnapshot
			{
				AccountId = 1,
				Date = new DateOnly(2024, 1, 1),
				Quantity = 10,
				Currency = Currency.USD,
				AverageCostPrice = 100,
				CurrentUnitPrice = 120,
				TotalInvested = 1000,
				TotalValue = 1200
			};

			var calculatorMock = new Mock<IPerformanceCalculator>();
			calculatorMock.Setup(x => x.GetCalculatedSnapshots(It.IsAny<Holding>(), Currency.USD))
				.ReturnsAsync(new List<CalculatedSnapshot> { updatedSnapshot });

			var loggerMock = new Mock<ILogger>();
			var appSettingsMock = new Mock<IApplicationSettings>();
			var settings = new Settings { PrimaryCurrency = "USD", DataProviderPreference = "YAHOO;COINGECKO" };
			var configInstance = new ConfigurationInstance { Settings = settings };
			appSettingsMock.Setup(x => x.ConfigurationInstance).Returns(configInstance);
			var task = new PerformanceTask(calculatorMock.Object, dbFactoryMock.Object, appSettingsMock.Object);

			// Act
			await task.DoWork(loggerMock.Object);

			// Assert
			var verifyContext = new DatabaseContext(options);
			var updatedHolding = await verifyContext.Holdings
				.Include(h => h.CalculatedSnapshots)
				.FirstAsync(h => h.Id == 1, TestContext.Current.CancellationToken);

			Assert.Single(updatedHolding.CalculatedSnapshots);
			Assert.Equal(10, updatedHolding.CalculatedSnapshots.First().Quantity);
			Assert.Equal(1200, updatedHolding.CalculatedSnapshots.First().TotalValue);

			await dbContext.DisposeAsync();
			await verifyContext.DisposeAsync();
		}

		[Fact]
		public async Task DoWork_RemovesObsoleteSnapshots()
		{
			using var connection = new SqliteConnection("Filename=:memory:");
			await connection.OpenAsync(TestContext.Current.CancellationToken);
			var options = CreateOptions(connection);
			var dbContext = new DatabaseContext(options);
			await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

			var holding = new Holding
			{
				Id = 1,
				SymbolProfiles =
				[
					new SymbolProfile { Symbol = "TEST", Name = "Test Holding", Currency = Currency.USD, DataSource = "YAHOO", AssetClass = AssetClass.Equity, CountryWeight = [], SectorWeights = [], Identifiers = [] }
				],
				CalculatedSnapshots =
				[
					new()
					{
						AccountId = 1,
						Date = new DateOnly(2024, 1, 1),
						Quantity = 5,
						Currency = Currency.USD,
						AverageCostPrice = 100,
						CurrentUnitPrice = 110,
						TotalInvested = 500,
						TotalValue = 550,
						HoldingId = 1
					},
					new()
					{
						AccountId = 1,
						Date = new DateOnly(2024, 1, 2),
						Quantity = 6,
						Currency = Currency.USD,
						AverageCostPrice = 100,
						CurrentUnitPrice = 115,
						TotalInvested = 600,
						TotalValue = 690,
						HoldingId = 1
					}
				]
			};
			dbContext.Holdings.Add(holding);
			await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

			var dbFactoryMock = new Mock<IDbContextFactory<DatabaseContext>>();
			dbFactoryMock.Setup(x => x.CreateDbContextAsync(default)).ReturnsAsync(() => new DatabaseContext(options));

			// Calculator only returns snapshot for 2024-01-01, so 2024-01-02 should be removed
			var newSnapshot = new CalculatedSnapshot
			{
				AccountId = 1,
				Date = new DateOnly(2024, 1, 1),
				Quantity = 10,
				Currency = Currency.USD,
				AverageCostPrice = 100,
				CurrentUnitPrice = 120,
				TotalInvested = 1000,
				TotalValue = 1200
			};

			var calculatorMock = new Mock<IPerformanceCalculator>();
			calculatorMock.Setup(x => x.GetCalculatedSnapshots(It.IsAny<Holding>(), Currency.USD))
				.ReturnsAsync(new List<CalculatedSnapshot> { newSnapshot });

			var loggerMock = new Mock<ILogger>();
			var appSettingsMock = new Mock<IApplicationSettings>();
			var settings = new Settings { PrimaryCurrency = "USD", DataProviderPreference = "YAHOO;COINGECKO" };
			var configInstance = new ConfigurationInstance { Settings = settings };
			appSettingsMock.Setup(x => x.ConfigurationInstance).Returns(configInstance);
			var task = new PerformanceTask(calculatorMock.Object, dbFactoryMock.Object, appSettingsMock.Object);

			// Act
			await task.DoWork(loggerMock.Object);

			// Assert
			var verifyContext = new DatabaseContext(options);
			var updatedHolding = await verifyContext.Holdings
				.Include(h => h.CalculatedSnapshots)
				.FirstAsync(h => h.Id == 1, TestContext.Current.CancellationToken);

			Assert.Single(updatedHolding.CalculatedSnapshots);
			Assert.Equal(new DateOnly(2024, 1, 1), updatedHolding.CalculatedSnapshots.First().Date);

			await dbContext.DisposeAsync();
			await verifyContext.DisposeAsync();
		}

		[Fact]
		public async Task DoWork_SkipsHoldingsWithoutSymbolProfiles()
		{
			using var connection = new SqliteConnection("Filename=:memory:");
			await connection.OpenAsync(TestContext.Current.CancellationToken);
			var options = CreateOptions(connection);
			var dbContext = new DatabaseContext(options);
			await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

			// Holding with no SymbolProfiles — should be skipped
			var holding = new Holding { Id = 1 };
			dbContext.Holdings.Add(holding);
			await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

			var dbFactoryMock = new Mock<IDbContextFactory<DatabaseContext>>();
			dbFactoryMock.Setup(x => x.CreateDbContextAsync(default)).ReturnsAsync(() => new DatabaseContext(options));

			var calculatorMock = new Mock<IPerformanceCalculator>();
			var loggerMock = new Mock<ILogger>();
			var appSettingsMock = new Mock<IApplicationSettings>();
			var settings = new Settings { PrimaryCurrency = "USD", DataProviderPreference = "YAHOO;COINGECKO" };
			var configInstance = new ConfigurationInstance { Settings = settings };
			appSettingsMock.Setup(x => x.ConfigurationInstance).Returns(configInstance);
			var task = new PerformanceTask(calculatorMock.Object, dbFactoryMock.Object, appSettingsMock.Object);

			// Act
			await task.DoWork(loggerMock.Object);

			// Assert — calculator never called, no snapshots written
			calculatorMock.Verify(x => x.GetCalculatedSnapshots(It.IsAny<Holding>(), It.IsAny<Currency>()), Times.Never);
			var verifyContext = new DatabaseContext(options);
			var snapshotCount = await verifyContext.CalculatedSnapshots.CountAsync(TestContext.Current.CancellationToken);
			Assert.Equal(0, snapshotCount);
			await dbContext.DisposeAsync();
			await verifyContext.DisposeAsync();
		}

		[Fact]
		public async Task DoWork_HandlesNoHoldings()
		{
			using var connection = new SqliteConnection("Filename=:memory:");
			await connection.OpenAsync(TestContext.Current.CancellationToken);
			var options = CreateOptions(connection);
			var dbContext = new DatabaseContext(options);
			await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
			// No holdings in DB

			var dbFactoryMock = new Mock<IDbContextFactory<DatabaseContext>>();
			dbFactoryMock.Setup(x => x.CreateDbContextAsync(default)).ReturnsAsync(() => new DatabaseContext(options));

			var calculatorMock = new Mock<IPerformanceCalculator>();
			var loggerMock = new Mock<ILogger>();
			var appSettingsMock = new Mock<IApplicationSettings>();
			var settings = new Settings { PrimaryCurrency = "USD", DataProviderPreference = "YAHOO;COINGECKO" };
			var configInstance = new ConfigurationInstance { Settings = settings };
			appSettingsMock.Setup(x => x.ConfigurationInstance).Returns(configInstance);
			var task = new PerformanceTask(calculatorMock.Object, dbFactoryMock.Object, appSettingsMock.Object);

			// Act
			await task.DoWork(loggerMock.Object);

			// Assert - should complete without error
			calculatorMock.Verify(x => x.GetCalculatedSnapshots(It.IsAny<Holding>(), Currency.USD), Times.Never);
			await dbContext.DisposeAsync();
		}

		[Fact]
		public async Task DoWork_ProcessesMultipleHoldings()
		{
			using var connection = new SqliteConnection("Filename=:memory:");
			await connection.OpenAsync(TestContext.Current.CancellationToken);
			var options = CreateOptions(connection);
			var dbContext = new DatabaseContext(options);
			await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

			dbContext.Holdings.AddRange(
				new Holding
				{
					Id = 1,
					SymbolProfiles =
					[
						new SymbolProfile { Symbol = "AAA", Name = "Holding A", Currency = Currency.USD, DataSource = "YAHOO", AssetClass = AssetClass.Equity, CountryWeight = [], SectorWeights = [], Identifiers = [] }
					]
				},
				new Holding
				{
					Id = 2,
					SymbolProfiles =
					[
						new SymbolProfile { Symbol = "BBB", Name = "Holding B", Currency = Currency.USD, DataSource = "YAHOO", AssetClass = AssetClass.Equity, CountryWeight = [], SectorWeights = [], Identifiers = [] }
					]
				});
			await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

			var dbFactoryMock = new Mock<IDbContextFactory<DatabaseContext>>();
			dbFactoryMock.Setup(x => x.CreateDbContextAsync(default)).ReturnsAsync(() => new DatabaseContext(options));

			var calculatorMock = new Mock<IPerformanceCalculator>();
			calculatorMock.Setup(x => x.GetCalculatedSnapshots(It.IsAny<Holding>(), Currency.USD))
				.ReturnsAsync(() => new List<CalculatedSnapshot>
				{
					new() { AccountId = 1, Date = new DateOnly(2024, 1, 1), Quantity = 5, Currency = Currency.USD, AverageCostPrice = 100, CurrentUnitPrice = 110, TotalInvested = 500, TotalValue = 550 }
				});

			var loggerMock = new Mock<ILogger>();
			var appSettingsMock = new Mock<IApplicationSettings>();
			var settings = new Settings { PrimaryCurrency = "USD", DataProviderPreference = "YAHOO;COINGECKO" };
			var configInstance = new ConfigurationInstance { Settings = settings };
			appSettingsMock.Setup(x => x.ConfigurationInstance).Returns(configInstance);
			var task = new PerformanceTask(calculatorMock.Object, dbFactoryMock.Object, appSettingsMock.Object);

			// Act
			await task.DoWork(loggerMock.Object);

			// Assert
			calculatorMock.Verify(x => x.GetCalculatedSnapshots(It.IsAny<Holding>(), Currency.USD), Times.Exactly(2));
			var verifyContext = new DatabaseContext(options);
			Assert.Equal(2, await verifyContext.CalculatedSnapshots.CountAsync(TestContext.Current.CancellationToken));
			await dbContext.DisposeAsync();
			await verifyContext.DisposeAsync();
		}

		[Fact]
		public async Task DoWork_ContinuesAfterCalculatorThrowsException()
		{
			using var connection = new SqliteConnection("Filename=:memory:");
			await connection.OpenAsync(TestContext.Current.CancellationToken);
			var options = CreateOptions(connection);
			var dbContext = new DatabaseContext(options);
			await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

			dbContext.Holdings.AddRange(
				new Holding
				{
					Id = 1,
					SymbolProfiles =
					[
						new SymbolProfile { Symbol = "AAA", Name = "Holding A", Currency = Currency.USD, DataSource = "YAHOO", AssetClass = AssetClass.Equity, CountryWeight = [], SectorWeights = [], Identifiers = [] }
					]
				},
				new Holding
				{
					Id = 2,
					SymbolProfiles =
					[
						new SymbolProfile { Symbol = "BBB", Name = "Holding B", Currency = Currency.USD, DataSource = "YAHOO", AssetClass = AssetClass.Equity, CountryWeight = [], SectorWeights = [], Identifiers = [] }
					]
				});
			await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

			var snapshot = new CalculatedSnapshot
			{
				AccountId = 1,
				Date = new DateOnly(2024, 1, 1),
				Quantity = 5,
				Currency = Currency.USD,
				AverageCostPrice = 100,
				CurrentUnitPrice = 110,
				TotalInvested = 500,
				TotalValue = 550
			};

			var dbFactoryMock = new Mock<IDbContextFactory<DatabaseContext>>();
			dbFactoryMock.Setup(x => x.CreateDbContextAsync(default)).ReturnsAsync(() => new DatabaseContext(options));

			var calculatorMock = new Mock<IPerformanceCalculator>();
			calculatorMock.SetupSequence(x => x.GetCalculatedSnapshots(It.IsAny<Holding>(), Currency.USD))
				.ThrowsAsync(new InvalidOperationException("Calculation failed"))
				.ReturnsAsync(new List<CalculatedSnapshot> { snapshot });

			var loggerMock = new Mock<ILogger>();
			var appSettingsMock = new Mock<IApplicationSettings>();
			var settings = new Settings { PrimaryCurrency = "USD", DataProviderPreference = "YAHOO;COINGECKO" };
			var configInstance = new ConfigurationInstance { Settings = settings };
			appSettingsMock.Setup(x => x.ConfigurationInstance).Returns(configInstance);
			var task = new PerformanceTask(calculatorMock.Object, dbFactoryMock.Object, appSettingsMock.Object);

			// Act — must not throw even though calculator fails for holding 1
			await task.DoWork(loggerMock.Object);

			// Assert — calculator attempted for both holdings; only holding 2's snapshot persisted
			calculatorMock.Verify(x => x.GetCalculatedSnapshots(It.IsAny<Holding>(), Currency.USD), Times.Exactly(2));
			var verifyContext = new DatabaseContext(options);
			Assert.Equal(1, await verifyContext.CalculatedSnapshots.CountAsync(TestContext.Current.CancellationToken));
			await dbContext.DisposeAsync();
			await verifyContext.DisposeAsync();
		}

		[Fact]
		public async Task DoWork_CalculatorReturnsEmptySnapshotsList()
		{
			using var connection = new SqliteConnection("Filename=:memory:");
			await connection.OpenAsync(TestContext.Current.CancellationToken);
			var options = CreateOptions(connection);
			var dbContext = new DatabaseContext(options);
			await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

			var holding = new Holding
			{
				Id = 1,
				SymbolProfiles =
				[
					new SymbolProfile { Symbol = "TEST", Name = "Test Holding", Currency = Currency.USD, DataSource = "YAHOO", AssetClass = AssetClass.Equity, CountryWeight = [], SectorWeights = [], Identifiers = [] }
				]
			};
			dbContext.Holdings.Add(holding);
			await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

			var dbFactoryMock = new Mock<IDbContextFactory<DatabaseContext>>();
			dbFactoryMock.Setup(x => x.CreateDbContextAsync(default)).ReturnsAsync(() => new DatabaseContext(options));

			var calculatorMock = new Mock<IPerformanceCalculator>();
			calculatorMock.Setup(x => x.GetCalculatedSnapshots(It.IsAny<Holding>(), Currency.USD))
				.ReturnsAsync(new List<CalculatedSnapshot>());

			var loggerMock = new Mock<ILogger>();
			var appSettingsMock = new Mock<IApplicationSettings>();
			var settings = new Settings { PrimaryCurrency = "USD", DataProviderPreference = "YAHOO;COINGECKO" };
			var configInstance = new ConfigurationInstance { Settings = settings };
			appSettingsMock.Setup(x => x.ConfigurationInstance).Returns(configInstance);
			var task = new PerformanceTask(calculatorMock.Object, dbFactoryMock.Object, appSettingsMock.Object);

			// Act
			await task.DoWork(loggerMock.Object);

			// Assert
			var verifyContext = new DatabaseContext(options);
			Assert.Equal(0, await verifyContext.CalculatedSnapshots.CountAsync(TestContext.Current.CancellationToken));
			await dbContext.DisposeAsync();
			await verifyContext.DisposeAsync();
		}

		[Fact]
		public async Task DoWork_PersistsMultipleSnapshotsFromCalculator()
		{
			using var connection = new SqliteConnection("Filename=:memory:");
			await connection.OpenAsync(TestContext.Current.CancellationToken);
			var options = CreateOptions(connection);
			var dbContext = new DatabaseContext(options);
			await dbContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

			var holding = new Holding
			{
				Id = 1,
				SymbolProfiles =
				[
					new SymbolProfile { Symbol = "TEST", Name = "Test Holding", Currency = Currency.USD, DataSource = "YAHOO", AssetClass = AssetClass.Equity, CountryWeight = [], SectorWeights = [], Identifiers = [] }
				]
			};
			dbContext.Holdings.Add(holding);
			await dbContext.SaveChangesAsync(TestContext.Current.CancellationToken);

			var dbFactoryMock = new Mock<IDbContextFactory<DatabaseContext>>();
			dbFactoryMock.Setup(x => x.CreateDbContextAsync(default)).ReturnsAsync(() => new DatabaseContext(options));

			var calculatorMock = new Mock<IPerformanceCalculator>();
			calculatorMock.Setup(x => x.GetCalculatedSnapshots(It.IsAny<Holding>(), Currency.USD))
				.ReturnsAsync(new List<CalculatedSnapshot>
				{
					new() { AccountId = 1, Date = new DateOnly(2024, 1, 1), Quantity = 5, Currency = Currency.USD, AverageCostPrice = 100, CurrentUnitPrice = 110, TotalInvested = 500, TotalValue = 550 },
					new() { AccountId = 1, Date = new DateOnly(2024, 1, 2), Quantity = 7, Currency = Currency.USD, AverageCostPrice = 100, CurrentUnitPrice = 115, TotalInvested = 700, TotalValue = 805 },
					new() { AccountId = 1, Date = new DateOnly(2024, 1, 3), Quantity = 10, Currency = Currency.USD, AverageCostPrice = 100, CurrentUnitPrice = 120, TotalInvested = 1000, TotalValue = 1200 }
				});

			var loggerMock = new Mock<ILogger>();
			var appSettingsMock = new Mock<IApplicationSettings>();
			var settings = new Settings { PrimaryCurrency = "USD", DataProviderPreference = "YAHOO;COINGECKO" };
			var configInstance = new ConfigurationInstance { Settings = settings };
			appSettingsMock.Setup(x => x.ConfigurationInstance).Returns(configInstance);
			var task = new PerformanceTask(calculatorMock.Object, dbFactoryMock.Object, appSettingsMock.Object);

			// Act
			await task.DoWork(loggerMock.Object);

			// Assert
			var verifyContext = new DatabaseContext(options);
			var snapshots = await verifyContext.CalculatedSnapshots.ToListAsync(TestContext.Current.CancellationToken);
			Assert.Equal(3, snapshots.Count);
			Assert.Contains(snapshots, s => s.Date == new DateOnly(2024, 1, 1));
			Assert.Contains(snapshots, s => s.Date == new DateOnly(2024, 1, 2));
			Assert.Contains(snapshots, s => s.Date == new DateOnly(2024, 1, 3));
			await dbContext.DisposeAsync();
			await verifyContext.DisposeAsync();
		}
	}
}

