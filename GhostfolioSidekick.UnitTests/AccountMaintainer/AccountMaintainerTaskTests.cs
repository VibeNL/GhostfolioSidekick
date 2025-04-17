using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using GhostfolioSidekick.Model;
using Moq.EntityFrameworkCore;
using GhostfolioSidekick.AccountMaintainer;

namespace GhostfolioSidekick.UnitTests.AccountMaintainer
{
	public class AccountMaintainerTaskTests
	{
		private readonly Mock<ILogger<AccountMaintainerTask>> mockLogger;
		private readonly Mock<IDbContextFactory<DatabaseContext>> mockDbContextFactory;
		private readonly Mock<IApplicationSettings> mockApplicationSettings;
		private readonly AccountMaintainerTask accountMaintainerTask;

		public AccountMaintainerTaskTests()
		{
			mockLogger = new Mock<ILogger<AccountMaintainerTask>>();
			mockDbContextFactory = new Mock<IDbContextFactory<DatabaseContext>>();
			mockApplicationSettings = new Mock<IApplicationSettings>();

			accountMaintainerTask = new AccountMaintainerTask(
				mockLogger.Object,
				mockDbContextFactory.Object,
				mockApplicationSettings.Object);
		}

		[Fact]
		public void Priority_ShouldReturnAccountMaintainer()
		{
			accountMaintainerTask.Priority.Should().Be(TaskPriority.AccountMaintainer);
		}

		[Fact]
		public void ExecutionFrequency_ShouldReturnOneHour()
		{
			accountMaintainerTask.ExecutionFrequency.Should().Be(TimeSpan.FromHours(1));
		}

		[Fact]
		public async Task DoWork_ShouldLogErrorOnException()
		{
			mockApplicationSettings.Setup(x => x.ConfigurationInstance).Throws(new Exception("Test exception"));

			await accountMaintainerTask.DoWork();

			mockLogger.VerifyLog(logger => logger.LogDebug("{Name} Starting to do work", nameof(AccountMaintainerTask)), Times.Once);
		}

		[Fact]
		public async Task AddOrUpdateAccountsAndPlatforms_ShouldCreateAccountIfNotExists()
		{
			// Arrange
			var accountConfig = new AccountConfiguration { Name = "TestAccount", Platform = "TestPlatform", Currency = Currency.USD.ToString() };
			var platformConfig = new PlatformConfiguration { Name = "TestPlatform" };
			var configurationInstance = new ConfigurationInstance
			{
				Accounts = [accountConfig],
				Platforms = [platformConfig]
			};

			mockApplicationSettings.Setup(x => x.ConfigurationInstance).Returns(configurationInstance);

			var mockDbContext = new Mock<DatabaseContext>();
			mockDbContext.Setup(x => x.Accounts).ReturnsDbSet([]);
			mockDbContextFactory.Setup(x => x.CreateDbContext()).Returns(mockDbContext.Object);

			// Act
			await accountMaintainerTask.DoWork();

			// Assert
			mockDbContext.Verify(x => x.Accounts.AddAsync(It.IsAny<Account>(), default), Times.Once);
			mockDbContext.Verify(x => x.SaveChangesAsync(default), Times.Once);
		}

		[Fact]
		public async Task DoWork_ShouldHandleDifferentAccountConfigurations()
		{
			// Arrange
			var accountConfig1 = new AccountConfiguration { Name = "Account1", Platform = "Platform1", Currency = Currency.USD.ToString() };
			var accountConfig2 = new AccountConfiguration { Name = "Account2", Platform = "Platform2", Currency = Currency.EUR.ToString() };
			var platformConfig1 = new PlatformConfiguration { Name = "Platform1" };
			var platformConfig2 = new PlatformConfiguration { Name = "Platform2" };
			var configurationInstance = new ConfigurationInstance
			{
				Accounts = [accountConfig1, accountConfig2],
				Platforms = [platformConfig1, platformConfig2]
			};

			mockApplicationSettings.Setup(x => x.ConfigurationInstance).Returns(configurationInstance);

			var mockDbContext = new Mock<DatabaseContext>();
			mockDbContext.Setup(x => x.Accounts).ReturnsDbSet([]);
			mockDbContextFactory.Setup(x => x.CreateDbContext()).Returns(mockDbContext.Object);

			// Act
			await accountMaintainerTask.DoWork();

			// Assert
			mockDbContext.Verify(x => x.Accounts.AddAsync(It.IsAny<Account>(), default), Times.Exactly(2));
			mockDbContext.Verify(x => x.SaveChangesAsync(default), Times.Exactly(2));
		}

		[Fact]
		public async Task AddOrUpdateAccountsAndPlatforms_ShouldHandleMissingPlatforms()
		{
			// Arrange
			var accountConfig = new AccountConfiguration { Name = "TestAccount", Platform = "MissingPlatform", Currency = Currency.USD.ToString() };
			var configurationInstance = new ConfigurationInstance
			{
				Accounts = [accountConfig],
				Platforms = []
			};

			mockApplicationSettings.Setup(x => x.ConfigurationInstance).Returns(configurationInstance);

			var mockDbContext = new Mock<DatabaseContext>();
			mockDbContext.Setup(x => x.Accounts).ReturnsDbSet([]);
			mockDbContextFactory.Setup(x => x.CreateDbContext()).Returns(mockDbContext.Object);

			// Act
			await accountMaintainerTask.DoWork();

			// Assert
			mockDbContext.Verify(x => x.Accounts.AddAsync(It.IsAny<Account>(), default), Times.Once);
			mockDbContext.Verify(x => x.SaveChangesAsync(default), Times.Once);
		}
	}
}
