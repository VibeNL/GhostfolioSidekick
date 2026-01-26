using GhostfolioSidekick.Model;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model.Accounts;
using Microsoft.Data.Sqlite;

namespace GhostfolioSidekick.UnitTests.Performance
{
	public class CovertToPrimaryCurrencyTaskTests
	{
		[Fact]
		public async Task DoWork_UsesRealSqliteDatabase()
		{
			var connection = new SqliteConnection("Filename=:memory:");
			await connection.OpenAsync();

			var options = new DbContextOptionsBuilder<DatabaseContext>()
				.UseSqlite(connection)
				.Options;

			// Use a context for seeding
			var seedContext = new DatabaseContext(options);
			await seedContext.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);

			// Seed Account entity to satisfy foreign key constraint
			seedContext.Accounts.Add(new Account { Id = 1, Name = "Test Account" });
			await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);

			// Seed data as needed for your test
			seedContext.Balances.Add(new Balance(DateOnly.FromDateTime(DateTime.Today), Money.Zero(Currency.EUR)) { AccountId = 1 });
			await seedContext.SaveChangesAsync(TestContext.Current.CancellationToken);
			await seedContext.DisposeAsync();

			var currencyExchangeMock = new Mock<ICurrencyExchange>();
			currencyExchangeMock.Setup(e => e.PreloadAllExchangeRates()).Returns(Task.CompletedTask);
			currencyExchangeMock.Setup(e => e.ConvertMoney(It.IsAny<Money>(), It.IsAny<Currency>(), It.IsAny<DateOnly>())).ReturnsAsync(Money.Zero(Currency.EUR));

			var appSettingsMock = new Mock<IApplicationSettings>();
			var configInstance = new ConfigurationInstance { Settings = new Settings { PrimaryCurrency = "EUR" } };
			appSettingsMock.Setup(a => a.ConfigurationInstance).Returns(configInstance);

			var dbContextFactoryMock = new Mock<IDbContextFactory<DatabaseContext>>();
			dbContextFactoryMock.Setup(f => f.CreateDbContextAsync()).ReturnsAsync(() => new DatabaseContext(options));

			var loggerMock = new Mock<ILogger>();

			var task = new CovertToPrimaryCurrencyTask(
				currencyExchangeMock.Object,
				dbContextFactoryMock.Object,
				appSettingsMock.Object
			);

			await task.DoWork(loggerMock.Object);

			// Use a new context to assert results
			await using var assertContext = new DatabaseContext(options);
			Assert.True(await assertContext.Balances.AnyAsync(TestContext.Current.CancellationToken));

			await connection.DisposeAsync();
		}
	}
}

