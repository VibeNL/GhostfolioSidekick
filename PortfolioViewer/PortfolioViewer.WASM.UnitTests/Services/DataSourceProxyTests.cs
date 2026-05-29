using AwesomeAssertions;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using Moq;

namespace GhostfolioSidekick.PortfolioViewer.WASM.UnitTests.Services
{
	/// <summary>
	/// Verifies that all data-service proxy classes correctly delegate to the local or API
	/// implementation based on <see cref="IDataSourceService.UseApiDirectly"/>.
	/// </summary>
	public class DataSourceProxyTests
	{
		// ─── HoldingsDataServiceProxy ────────────────────────────────────────────

		[Fact]
		public async Task HoldingsProxy_WhenUseApiDirectly_False_DelegatesToLocalService()
		{
			var local = new Mock<IHoldingsDataService>();
			var api = new Mock<IHoldingsDataService>();
			var ds = new DataSourceService { UseApiDirectly = false };
			var proxy = new HoldingsDataServiceProxy(local.Object, api.Object, ds);

			local.Setup(x => x.GetHoldingsAsync(It.IsAny<CancellationToken>()))
				 .ReturnsAsync([]);

			await proxy.GetHoldingsAsync(CancellationToken.None);

			local.Verify(x => x.GetHoldingsAsync(It.IsAny<CancellationToken>()), Times.Once);
			api.Verify(x => x.GetHoldingsAsync(It.IsAny<CancellationToken>()), Times.Never);
		}

		[Fact]
		public async Task HoldingsProxy_WhenUseApiDirectly_True_DelegatesToApiService()
		{
			var local = new Mock<IHoldingsDataService>();
			var api = new Mock<IHoldingsDataService>();
			var ds = new DataSourceService { UseApiDirectly = true };
			var proxy = new HoldingsDataServiceProxy(local.Object, api.Object, ds);

			api.Setup(x => x.GetHoldingsAsync(It.IsAny<CancellationToken>()))
			   .ReturnsAsync([]);

			await proxy.GetHoldingsAsync(CancellationToken.None);

			api.Verify(x => x.GetHoldingsAsync(It.IsAny<CancellationToken>()), Times.Once);
			local.Verify(x => x.GetHoldingsAsync(It.IsAny<CancellationToken>()), Times.Never);
		}

		[Fact]
		public async Task HoldingsProxy_SwitchingMode_RoutesCorrectly()
		{
			var local = new Mock<IHoldingsDataService>();
			var api = new Mock<IHoldingsDataService>();
			var ds = new DataSourceService { UseApiDirectly = false };
			var proxy = new HoldingsDataServiceProxy(local.Object, api.Object, ds);

			local.Setup(x => x.GetHoldingsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);
			api.Setup(x => x.GetHoldingsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

			await proxy.GetHoldingsAsync(CancellationToken.None);
			ds.UseApiDirectly = true;
			await proxy.GetHoldingsAsync(CancellationToken.None);

			local.Verify(x => x.GetHoldingsAsync(It.IsAny<CancellationToken>()), Times.Once);
			api.Verify(x => x.GetHoldingsAsync(It.IsAny<CancellationToken>()), Times.Once);
		}

		// ─── AccountDataServiceProxy ─────────────────────────────────────────────

		[Fact]
		public async Task AccountProxy_WhenUseApiDirectly_False_DelegatesToLocalService()
		{
			var local = new Mock<IAccountDataService>();
			var api = new Mock<IAccountDataService>();
			var ds = new DataSourceService { UseApiDirectly = false };
			var proxy = new AccountDataServiceProxy(local.Object, api.Object, ds);

			local.Setup(x => x.GetAccountInfo()).ReturnsAsync([]);

			await proxy.GetAccountInfo();

			local.Verify(x => x.GetAccountInfo(), Times.Once);
			api.Verify(x => x.GetAccountInfo(), Times.Never);
		}

		[Fact]
		public async Task AccountProxy_WhenUseApiDirectly_True_DelegatesToApiService()
		{
			var local = new Mock<IAccountDataService>();
			var api = new Mock<IAccountDataService>();
			var ds = new DataSourceService { UseApiDirectly = true };
			var proxy = new AccountDataServiceProxy(local.Object, api.Object, ds);

			api.Setup(x => x.GetAccountInfo()).ReturnsAsync([]);

			await proxy.GetAccountInfo();

			api.Verify(x => x.GetAccountInfo(), Times.Once);
			local.Verify(x => x.GetAccountInfo(), Times.Never);
		}

		// ─── TransactionServiceProxy ─────────────────────────────────────────────

		[Fact]
		public async Task TransactionProxy_WhenUseApiDirectly_False_DelegatesToLocalService()
		{
			var local = new Mock<ITransactionService>();
			var api = new Mock<ITransactionService>();
			var ds = new DataSourceService { UseApiDirectly = false };
			var proxy = new TransactionServiceProxy(local.Object, api.Object, ds);
			var parameters = new TransactionQueryParameters();

			local.Setup(x => x.GetTransactionsPaginatedAsync(parameters, It.IsAny<CancellationToken>()))
				 .ReturnsAsync(new PaginatedTransactionResult());

			await proxy.GetTransactionsPaginatedAsync(parameters, CancellationToken.None);

			local.Verify(x => x.GetTransactionsPaginatedAsync(parameters, It.IsAny<CancellationToken>()), Times.Once);
			api.Verify(x => x.GetTransactionsPaginatedAsync(It.IsAny<TransactionQueryParameters>(), It.IsAny<CancellationToken>()), Times.Never);
		}

		[Fact]
		public async Task TransactionProxy_WhenUseApiDirectly_True_DelegatesToApiService()
		{
			var local = new Mock<ITransactionService>();
			var api = new Mock<ITransactionService>();
			var ds = new DataSourceService { UseApiDirectly = true };
			var proxy = new TransactionServiceProxy(local.Object, api.Object, ds);
			var parameters = new TransactionQueryParameters();

			api.Setup(x => x.GetTransactionsPaginatedAsync(It.IsAny<TransactionQueryParameters>(), It.IsAny<CancellationToken>()))
			   .ReturnsAsync(new PaginatedTransactionResult());

			await proxy.GetTransactionsPaginatedAsync(parameters, CancellationToken.None);

			api.Verify(x => x.GetTransactionsPaginatedAsync(It.IsAny<TransactionQueryParameters>(), It.IsAny<CancellationToken>()), Times.Once);
			local.Verify(x => x.GetTransactionsPaginatedAsync(It.IsAny<TransactionQueryParameters>(), It.IsAny<CancellationToken>()), Times.Never);
		}

		// ─── DataIssuesServiceProxy ───────────────────────────────────────────────

		[Fact]
		public async Task DataIssuesProxy_WhenUseApiDirectly_False_DelegatesToLocalService()
		{
			var local = new Mock<IDataIssuesService>();
			var api = new Mock<IDataIssuesService>();
			var ds = new DataSourceService { UseApiDirectly = false };
			var proxy = new DataIssuesServiceProxy(local.Object, api.Object, ds);

			local.Setup(x => x.GetActivitiesWithoutHoldingsAsync(It.IsAny<CancellationToken>()))
				 .ReturnsAsync([]);

			await proxy.GetActivitiesWithoutHoldingsAsync(CancellationToken.None);

			local.Verify(x => x.GetActivitiesWithoutHoldingsAsync(It.IsAny<CancellationToken>()), Times.Once);
			api.Verify(x => x.GetActivitiesWithoutHoldingsAsync(It.IsAny<CancellationToken>()), Times.Never);
		}

		[Fact]
		public async Task DataIssuesProxy_WhenUseApiDirectly_True_DelegatesToApiService()
		{
			var local = new Mock<IDataIssuesService>();
			var api = new Mock<IDataIssuesService>();
			var ds = new DataSourceService { UseApiDirectly = true };
			var proxy = new DataIssuesServiceProxy(local.Object, api.Object, ds);

			api.Setup(x => x.GetActivitiesWithoutHoldingsAsync(It.IsAny<CancellationToken>()))
			   .ReturnsAsync([]);

			await proxy.GetActivitiesWithoutHoldingsAsync(CancellationToken.None);

			api.Verify(x => x.GetActivitiesWithoutHoldingsAsync(It.IsAny<CancellationToken>()), Times.Once);
			local.Verify(x => x.GetActivitiesWithoutHoldingsAsync(It.IsAny<CancellationToken>()), Times.Never);
		}

		// ─── UpcomingDividendsServiceProxy ────────────────────────────────────────

		[Fact]
		public async Task UpcomingDividendsProxy_WhenUseApiDirectly_False_DelegatesToLocalService()
		{
			var local = new Mock<IUpcomingDividendsService>();
			var api = new Mock<IUpcomingDividendsService>();
			var ds = new DataSourceService { UseApiDirectly = false };
			var proxy = new UpcomingDividendsServiceProxy(local.Object, api.Object, ds);

			local.Setup(x => x.GetUpcomingDividendsAsync()).ReturnsAsync([]);

			await proxy.GetUpcomingDividendsAsync();

			local.Verify(x => x.GetUpcomingDividendsAsync(), Times.Once);
			api.Verify(x => x.GetUpcomingDividendsAsync(), Times.Never);
		}

		[Fact]
		public async Task UpcomingDividendsProxy_WhenUseApiDirectly_True_DelegatesToApiService()
		{
			var local = new Mock<IUpcomingDividendsService>();
			var api = new Mock<IUpcomingDividendsService>();
			var ds = new DataSourceService { UseApiDirectly = true };
			var proxy = new UpcomingDividendsServiceProxy(local.Object, api.Object, ds);

			api.Setup(x => x.GetUpcomingDividendsAsync()).ReturnsAsync([]);

			await proxy.GetUpcomingDividendsAsync();

			api.Verify(x => x.GetUpcomingDividendsAsync(), Times.Once);
			local.Verify(x => x.GetUpcomingDividendsAsync(), Times.Never);
		}
	}
}
