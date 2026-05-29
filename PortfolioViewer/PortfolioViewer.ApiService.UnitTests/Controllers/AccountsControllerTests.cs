using AwesomeAssertions;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.PortfolioViewer.ApiService.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Xunit;

namespace GhostfolioSidekick.PortfolioViewer.ApiService.UnitTests.Controllers
{
	public class AccountsControllerTests : IDisposable
	{
		private readonly DatabaseContext _db;
		private readonly Mock<IDbContextFactory<DatabaseContext>> _dbFactoryMock;
		private readonly Mock<IApplicationSettings> _appSettingsMock;
		private readonly AccountsController _controller;

		public AccountsControllerTests()
		{
			var options = new DbContextOptionsBuilder<DatabaseContext>()
				.UseInMemoryDatabase(Guid.NewGuid().ToString())
				.Options;
			_db = new DatabaseContext(options);

			_dbFactoryMock = new Mock<IDbContextFactory<DatabaseContext>>();
			_dbFactoryMock
				.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
				.ReturnsAsync(_db);

			_appSettingsMock = new Mock<IApplicationSettings>();
			_appSettingsMock.Setup(x => x.ConfigurationInstance).Returns(
				new ConfigurationInstance
				{
					Settings = new Settings
					{
						DataProviderPreference = "YAHOO",
						PrimaryCurrency = "USD"
					}
				});

			_controller = new AccountsController(_dbFactoryMock.Object, _appSettingsMock.Object);
		}

		public void Dispose() => _db.Dispose();

		private static Platform CreatePlatform(string name)
		{
			return new Platform(name);
		}

		private static Account CreateAccount(string name, Platform? platform = null)
		{
			return new Account(name) { Platform = platform };
		}

		[Fact]
		public async Task GetAccounts_ReturnsOk_WithEmptyDatabase()
		{
			var result = await _controller.GetAccounts(null, CancellationToken.None);

			result.Should().BeOfType<OkObjectResult>();
		}

		[Fact]
		public async Task GetAccounts_ReturnsAccounts_WhenAccountsExist()
		{
			var platform = CreatePlatform("Test Platform");
			var account = CreateAccount("My Account", platform);
			await _db.Accounts.AddAsync(account, TestContext.Current.CancellationToken);
			await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

			var result = await _controller.GetAccounts(null, CancellationToken.None);

			result.Should().BeOfType<OkObjectResult>();
		}

		[Fact]
		public async Task GetAccountById_ReturnsNotFound_WhenAccountDoesNotExist()
		{
			var result = await _controller.GetAccountById(9999, CancellationToken.None);

			result.Should().BeOfType<NotFoundResult>();
		}

		[Fact]
		public async Task GetAccountById_ReturnsOk_WhenAccountExists()
		{
			var account = CreateAccount("Alpha");
			await _db.Accounts.AddAsync(account, TestContext.Current.CancellationToken);
			await _db.SaveChangesAsync(TestContext.Current.CancellationToken);

			var result = await _controller.GetAccountById(account.Id, CancellationToken.None);

			result.Should().BeOfType<OkObjectResult>();
		}

		[Fact]
		public async Task GetSymbolProfiles_ReturnsOk_WithEmptyDatabase()
		{
			var result = await _controller.GetSymbolProfiles(null, CancellationToken.None);

			result.Should().BeOfType<OkObjectResult>();
		}

		[Fact]
		public async Task GetTaxReport_ReturnsOk_WithEmptyDatabase()
		{
			var result = await _controller.GetTaxReport(CancellationToken.None);

			result.Should().BeOfType<OkObjectResult>();
		}
	}
}
