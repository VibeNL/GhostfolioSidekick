using AwesomeAssertions;
using GhostfolioSidekick.AccountMaintainer;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
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
			var accountConfig = new AccountConfiguration
			{
				Name = "TestAccount",
				Platform = "TestPlatform",
				Currency = Currency.USD.ToString(),
				SyncActivities = false,
				SyncBalance = true
			};
			var platformConfig = new PlatformConfiguration { Name = "TestPlatform" };
			var configurationInstance = new ConfigurationInstance
			{
				Accounts = [accountConfig],
				Platforms = [platformConfig]
			};

			mockApplicationSettings.Setup(x => x.ConfigurationInstance).Returns(configurationInstance);
			mockApplicationSettings.Setup(x => x.AllowAdminCalls).Returns(true);

			var mockDbContext = new Mock<DatabaseContext>();
			mockDbContext.Setup(x => x.Accounts).ReturnsDbSet([]);
			mockDbContext.Setup(x => x.Platforms).ReturnsDbSet([]);
			mockDbContextFactory.Setup(x => x.CreateDbContext()).Returns(mockDbContext.Object);

			// Act
			await accountMaintainerTask.DoWork();

			// Assert
			mockDbContext.Verify(x => x.Accounts.AddAsync(It.Is<Account>(a =>
				a.Name == "TestAccount" &&
				!a.SyncActivities &&
				a.SyncBalance), default), Times.Once);
			mockDbContext.Verify(x => x.SaveChangesAsync(default), Times.AtLeastOnce);
		}

		[Fact]
		public async Task AddOrUpdateAccountsAndPlatforms_ShouldUpdateAccountIfExists()
		{
			// Arrange
			var existingAccount = new Account("TestAccount")
			{
				Id = 1,
				SyncActivities = true,
				SyncBalance = true
			};
			var accountConfig = new AccountConfiguration
			{
				Name = "TestAccount",
				Currency = Currency.USD.ToString(),
				SyncActivities = false,
				SyncBalance = false,
				Comment = "Updated comment"
			};
			var configurationInstance = new ConfigurationInstance
			{
				Accounts = [accountConfig],
				Platforms = []
			};

			mockApplicationSettings.Setup(x => x.ConfigurationInstance).Returns(configurationInstance);

			var mockDbContext = new Mock<DatabaseContext>();
			mockDbContext.Setup(x => x.Accounts).ReturnsDbSet([existingAccount]);
			mockDbContext.Setup(x => x.Platforms).ReturnsDbSet([]);
			mockDbContextFactory.Setup(x => x.CreateDbContext()).Returns(mockDbContext.Object);

			// Act
			await accountMaintainerTask.DoWork();

			// Assert
			existingAccount.SyncActivities.Should().BeFalse();
			existingAccount.SyncBalance.Should().BeFalse();
			existingAccount.Comment.Should().Be("Updated comment");
			mockDbContext.Verify(x => x.SaveChangesAsync(default), Times.Once);
		}
	}
}