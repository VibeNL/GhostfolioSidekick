using GhostfolioSidekick.AccountMaintainer;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;
using GhostfolioSidekick.Model;
using Moq.EntityFrameworkCore;

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
		public async Task DoWork_ShouldLogWarning_WhenConfigurationInstanceIsNull()
		{
			// Arrange
			mockApplicationSettings.Setup(x => x.ConfigurationInstance).Returns((ConfigurationInstance)null);

			// Act
			await accountMaintainerTask.DoWork();

			// Assert
			mockLogger.VerifyLog(logger => logger.LogWarning("{Name} ConfigurationInstance is null", nameof(AccountMaintainerTask)), Times.Once);
		}

		[Fact]
		public async Task AddOrUpdateAccountsAndPlatforms_ShouldUpdateAccountIfExists()
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

			var existingAccount = new Account("TestAccount");
			var mockDbContext = new Mock<DatabaseContext>();
			mockDbContext.Setup(x => x.Accounts).ReturnsDbSet([existingAccount]);
			mockDbContextFactory.Setup(x => x.CreateDbContext()).Returns(mockDbContext.Object);

			// Act
			await accountMaintainerTask.DoWork();

			// Assert
			mockDbContext.Verify(x => x.Accounts.Update(It.IsAny<Account>()), Times.Once);
			mockDbContext.Verify(x => x.SaveChangesAsync(default), Times.Once);
		}
	}
}
