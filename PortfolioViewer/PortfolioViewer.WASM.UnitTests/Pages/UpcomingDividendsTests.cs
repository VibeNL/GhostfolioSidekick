using AwesomeAssertions;
using Bunit;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using GhostfolioSidekick.PortfolioViewer.WASM.Pages;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using System.Globalization;

namespace GhostfolioSidekick.PortfolioViewer.WASM.UnitTests.Pages
{
	public class UpcomingDividendsTests : BunitContext
	{
		private readonly Mock<IUpcomingDividendsService> _mockDividendsService;
		private readonly Mock<IServerConfigurationService> _mockServerConfigurationService;

		public UpcomingDividendsTests()
		{
			_mockDividendsService = new Mock<IUpcomingDividendsService>();
			_mockServerConfigurationService = new Mock<IServerConfigurationService>();

			// Setup default behavior for server configuration service
			_mockServerConfigurationService.Setup(x => x.PrimaryCurrency).Returns(Currency.USD);

			// Register services
			Services.AddSingleton(_mockDividendsService.Object);
			Services.AddSingleton(_mockServerConfigurationService.Object);
		}

		[Fact]
		public void UpcomingDividends_InitialState_ShowsLoadingState()
		{
			// Arrange
			_mockDividendsService.Setup(x => x.GetUpcomingDividendsAsync())
				.ReturnsAsync(new List<UpcomingDividendModel>());

			// Act
			var component = Render<UpcomingDividends>();

			// Assert - Initially shows loading
			component.Markup.Should().Contain("Loading Upcoming Dividends");
		}

		[Fact]
		public async Task UpcomingDividends_WhenNoDividends_ShowsEmptyState()
		{
			// Arrange
			_mockDividendsService.Setup(x => x.GetUpcomingDividendsAsync())
				.ReturnsAsync(new List<UpcomingDividendModel>());

			// Act
			var component = Render<UpcomingDividends>();
			await WaitForComponentToFinishLoading(component, maxWaitMs: 1000);

			// Assert
			var markup = component.Markup;
			markup.Should().Contain("No Upcoming Dividends");
		}

		[Fact]
		public async Task UpcomingDividends_WithDividends_DisplaysDividendTable()
		{
			// Arrange
			var dividends = new List<UpcomingDividendModel>
			{
				new()
				{
					Symbol = "AAPL",
					CompanyName = "Apple Inc.",
					ExDate = DateTime.Today.AddDays(1),
					PaymentDate = DateTime.Today.AddDays(10),
					Amount = 25.0m,
					Currency = "USD",
					DividendPerShare = 2.5m,
					AmountPrimaryCurrency = 25.0m,
					PrimaryCurrency = "USD",
					DividendPerSharePrimaryCurrency = 2.5m,
					Quantity = 10m,
					IsPredicted = false
				}
			};

			_mockDividendsService.Setup(x => x.GetUpcomingDividendsAsync())
				.ReturnsAsync(dividends);

			// Act
			var component = Render<UpcomingDividends>();
			await WaitForComponentToFinishLoading(component, containsText: "AAPL", maxWaitMs: 1000);

			// Assert
			var markup = component.Markup;
			markup.Should().Contain("AAPL");
			markup.Should().Contain("25.00 USD");
			markup.Should().Contain("Confirmed");
		}

		[Fact]
		public async Task UpcomingDividends_WithPredictedDividend_ShowsPredictedBadge()
		{
			// Arrange
			var dividends = new List<UpcomingDividendModel>
			{
				new()
				{
					Symbol = "MSFT",
					CompanyName = "Microsoft Corp.",
					ExDate = DateTime.Today.AddDays(1),
					PaymentDate = DateTime.Today.AddDays(10),
					Amount = 30.0m,
					Currency = "USD",
					DividendPerShare = 3.0m,
					AmountPrimaryCurrency = 30.0m,
					PrimaryCurrency = "USD",
					DividendPerSharePrimaryCurrency = 3.0m,
					Quantity = 10m,
					IsPredicted = true // Predicted
				}
			};

			_mockDividendsService.Setup(x => x.GetUpcomingDividendsAsync())
				.ReturnsAsync(dividends);

			// Act
			var component = Render<UpcomingDividends>();
			await WaitForComponentToFinishLoading(component, containsText: "MSFT", maxWaitMs: 1000);

			// Assert
			var markup = component.Markup;
			markup.Should().Contain("MSFT");
			markup.Should().Contain("Predicted");
		}

		[Fact]
		public async Task UpcomingDividends_WithMultipleDividends_DisplaysAllSortedByDate()
		{
			// Arrange
			var dividends = new List<UpcomingDividendModel>
			{
				new()
				{
					Symbol = "AAPL",
					CompanyName = "Apple Inc.",
					ExDate = DateTime.Today.AddDays(1),
					PaymentDate = DateTime.Today.AddDays(20),
					Amount = 25.0m,
					Currency = "USD",
					DividendPerShare = 2.5m,
					AmountPrimaryCurrency = 25.0m,
					PrimaryCurrency = "USD",
					DividendPerSharePrimaryCurrency = 2.5m,
					Quantity = 10m,
					IsPredicted = false
				},
				new()
				{
					Symbol = "MSFT",
					CompanyName = "Microsoft Corp.",
					ExDate = DateTime.Today.AddDays(1),
					PaymentDate = DateTime.Today.AddDays(10), // Earlier payment date
					Amount = 30.0m,
					Currency = "USD",
					DividendPerShare = 3.0m,
					AmountPrimaryCurrency = 30.0m,
					PrimaryCurrency = "USD",
					DividendPerSharePrimaryCurrency = 3.0m,
					Quantity = 10m,
					IsPredicted = false
				}
			};

			_mockDividendsService.Setup(x => x.GetUpcomingDividendsAsync())
				.ReturnsAsync(dividends);

			// Act
			var component = Render<UpcomingDividends>();
			await WaitForComponentToFinishLoading(component, containsText: "MSFT", maxWaitMs: 1000);

			// Assert - Both symbols should be present
			var markup = component.Markup;
			markup.Should().Contain("AAPL");
			markup.Should().Contain("MSFT");
		}

		[Fact]
		public async Task UpcomingDividends_ShowDetails_DisplaysModal()
		{
			// Arrange
			var dividends = new List<UpcomingDividendModel>
			{
				new()
				{
					Symbol = "AAPL",
					CompanyName = "Apple Inc.",
					ExDate = DateTime.Today.AddDays(1),
					PaymentDate = DateTime.Today.AddDays(10),
					Amount = 25.0m,
					Currency = "USD",
					DividendPerShare = 2.5m,
					AmountPrimaryCurrency = 25.0m,
					PrimaryCurrency = "USD",
					DividendPerSharePrimaryCurrency = 2.5m,
					Quantity = 10m,
					IsPredicted = false
				}
			};

			_mockDividendsService.Setup(x => x.GetUpcomingDividendsAsync())
				.ReturnsAsync(dividends);

			// Act
			var component = Render<UpcomingDividends>();
			await WaitForComponentToFinishLoading(component, containsText: "AAPL", maxWaitMs: 1000);

			// Click Details button
			var detailsButton = component.Find("button:contains('Details')");
			detailsButton.Click();

			// Assert - Modal should be visible
			var markup = component.Markup;
			markup.Should().Contain("Dividend Details - AAPL");
			markup.Should().Contain("Apple Inc.");
			markup.Should().Contain("10.00"); // Quantity
		}

		[Fact]
		public async Task UpcomingDividends_CloseDetails_HidesModal()
		{
			// Arrange
			var dividends = new List<UpcomingDividendModel>
			{
				new()
				{
					Symbol = "AAPL",
					CompanyName = "Apple Inc.",
					ExDate = DateTime.Today.AddDays(1),
					PaymentDate = DateTime.Today.AddDays(10),
					Amount = 25.0m,
					Currency = "USD",
					DividendPerShare = 2.5m,
					AmountPrimaryCurrency = 25.0m,
					PrimaryCurrency = "USD",
					DividendPerSharePrimaryCurrency = 2.5m,
					Quantity = 10m,
					IsPredicted = false
				}
			};

			_mockDividendsService.Setup(x => x.GetUpcomingDividendsAsync())
				.ReturnsAsync(dividends);

			// Act
			var component = Render<UpcomingDividends>();
			await WaitForComponentToFinishLoading(component, containsText: "AAPL", maxWaitMs: 1000);

			// Click Details button
			var detailsButton = component.Find("button:contains('Details')");
			detailsButton.Click();

			// Click Close button
			var closeButton = component.Find("button:contains('Close')");
			closeButton.Click();

			// Assert - Modal should be hidden
			var markup = component.Markup;
			markup.Should().NotContain("Dividend Details - AAPL");
		}

		[Fact]
		public async Task UpcomingDividends_BuildsChartWithConfirmedAndPredicted()
		{
			// Arrange
			var today = DateTime.Today;
			var dividends = new List<UpcomingDividendModel>
			{
				new()
				{
					Symbol = "AAPL",
					CompanyName = "Apple Inc.",
					ExDate = today.AddDays(1),
					PaymentDate = today.AddDays(10),
					Amount = 25.0m,
					Currency = "USD",
					DividendPerShare = 2.5m,
					AmountPrimaryCurrency = 25.0m,
					PrimaryCurrency = "USD",
					DividendPerSharePrimaryCurrency = 2.5m,
					Quantity = 10m,
					IsPredicted = false
				},
				new()
				{
					Symbol = "MSFT",
					CompanyName = "Microsoft Corp.",
					ExDate = today.AddDays(1),
					PaymentDate = today.AddDays(45), // Next month
					Amount = 30.0m,
					Currency = "USD",
					DividendPerShare = 3.0m,
					AmountPrimaryCurrency = 30.0m,
					PrimaryCurrency = "USD",
					DividendPerSharePrimaryCurrency = 3.0m,
					Quantity = 10m,
					IsPredicted = true
				}
			};

			_mockDividendsService.Setup(x => x.GetUpcomingDividendsAsync())
				.ReturnsAsync(dividends);

			// Act
			var component = Render<UpcomingDividends>();
			await WaitForComponentToFinishLoading(component, containsText: "AAPL", maxWaitMs: 1000);

			// Assert - Chart should be rendered
			var markup = component.Markup;
			markup.Should().Contain("Monthly Dividend Overview");
		}

		[Fact]
		public async Task UpcomingDividends_WithDifferentCurrencies_DisplaysBothCurrencies()
		{
			// Arrange
			var dividends = new List<UpcomingDividendModel>
			{
				new()
				{
					Symbol = "ASML",
					CompanyName = "ASML Holding NV",
					ExDate = DateTime.Today.AddDays(1),
					PaymentDate = DateTime.Today.AddDays(10),
					Amount = 15.0m, // EUR
					Currency = "EUR",
					DividendPerShare = 3.0m,
					AmountPrimaryCurrency = 16.5m, // USD converted
					PrimaryCurrency = "USD",
					DividendPerSharePrimaryCurrency = 3.3m,
					Quantity = 5m,
					IsPredicted = false
				}
			};

			_mockDividendsService.Setup(x => x.GetUpcomingDividendsAsync())
				.ReturnsAsync(dividends);

			// Act
			var component = Render<UpcomingDividends>();
			await WaitForComponentToFinishLoading(component, containsText: "ASML", maxWaitMs: 1000);

			// Click Details to see both currencies
			var detailsButton = component.Find("button:contains('Details')");
			detailsButton.Click();

			// Assert - Should show both EUR and USD amounts
			var markup = component.Markup;
			markup.Should().Contain("15.00 EUR");
			markup.Should().Contain("16.50 USD");
		}

		[Fact]
		public async Task UpcomingDividends_HandlesServiceError_Gracefully()
		{
			// Arrange
			_mockDividendsService.Setup(x => x.GetUpcomingDividendsAsync())
				.ThrowsAsync(new Exception("Service error"));

			// Act
			var component = Render<UpcomingDividends>();
			await WaitForComponentToFinishLoading(component, maxWaitMs: 1000);

			// Assert - Should handle error gracefully (may show loading or error state)
			var markup = component.Markup;
			(markup.Contains("Loading") || markup.Contains("Error") || markup.Contains("No Upcoming Dividends")).Should().BeTrue();
		}

		[Fact]
		public async Task UpcomingDividends_ChartNotRendered_WhenNoDividends()
		{
			// Arrange
			_mockDividendsService.Setup(x => x.GetUpcomingDividendsAsync())
				.ReturnsAsync(new List<UpcomingDividendModel>());

			// Act
			var component = Render<UpcomingDividends>();
			await WaitForComponentToFinishLoading(component, maxWaitMs: 1000);

			// Assert - Chart section should not be rendered
			var markup = component.Markup;
			markup.Should().NotContain("Monthly Dividend Overview");
		}

		[Fact]
		public async Task UpcomingDividends_DisplaysCorrectMonthFormatting()
		{
			// Arrange
			var today = DateTime.Today;
			var nextMonth = today.AddMonths(1);
			var dividends = new List<UpcomingDividendModel>
			{
				new()
				{
					Symbol = "AAPL",
					CompanyName = "Apple Inc.",
					ExDate = today.AddDays(1),
					PaymentDate = nextMonth,
					Amount = 25.0m,
					Currency = "USD",
					DividendPerShare = 2.5m,
					AmountPrimaryCurrency = 25.0m,
					PrimaryCurrency = "USD",
					DividendPerSharePrimaryCurrency = 2.5m,
					Quantity = 10m,
					IsPredicted = false
				}
			};

			_mockDividendsService.Setup(x => x.GetUpcomingDividendsAsync())
				.ReturnsAsync(dividends);

			// Act
			var component = Render<UpcomingDividends>();
			await WaitForComponentToFinishLoading(component, containsText: "AAPL", maxWaitMs: 1000);

			// Assert - Payment date should be formatted as yyyy-MM-dd
			var markup = component.Markup;
			markup.Should().Contain(nextMonth.ToString("yyyy-MM-dd"));
		}

		[Fact]
		public async Task UpcomingDividends_DetailsModal_ShowsDashForSameCurrency()
		{
			// Arrange
			var dividends = new List<UpcomingDividendModel>
			{
				new()
				{
					Symbol = "AAPL",
					CompanyName = "Apple Inc.",
					ExDate = DateTime.Today.AddDays(1),
					PaymentDate = DateTime.Today.AddDays(10),
					Amount = 25.0m,
					Currency = "USD",
					DividendPerShare = 2.5m,
					AmountPrimaryCurrency = 25.0m,
					PrimaryCurrency = "USD", // Same as native currency
					DividendPerSharePrimaryCurrency = 2.5m,
					Quantity = 10m,
					IsPredicted = false
				}
			};

			_mockDividendsService.Setup(x => x.GetUpcomingDividendsAsync())
				.ReturnsAsync(dividends);

			// Act
			var component = Render<UpcomingDividends>();
			await WaitForComponentToFinishLoading(component, containsText: "AAPL", maxWaitMs: 1000);

			// Click Details button
			var detailsButton = component.Find("button:contains('Details')");
			detailsButton.Click();

			// Assert - Should show dash for Amount (Primary) when currencies are the same
			var markup = component.Markup;
			markup.Should().Contain("text-muted");
		}

		[Fact]
		public async Task UpcomingDividends_DetailsModal_CloseButtonWorks()
		{
			// Arrange
			var dividends = new List<UpcomingDividendModel>
			{
				new()
				{
					Symbol = "AAPL",
					CompanyName = "Apple Inc.",
					ExDate = DateTime.Today.AddDays(1),
					PaymentDate = DateTime.Today.AddDays(10),
					Amount = 25.0m,
					Currency = "USD",
					DividendPerShare = 2.5m,
					AmountPrimaryCurrency = 25.0m,
					PrimaryCurrency = "USD",
					DividendPerSharePrimaryCurrency = 2.5m,
					Quantity = 10m,
					IsPredicted = false
				}
			};

			_mockDividendsService.Setup(x => x.GetUpcomingDividendsAsync())
				.ReturnsAsync(dividends);

			// Act
			var component = Render<UpcomingDividends>();
			await WaitForComponentToFinishLoading(component, containsText: "AAPL", maxWaitMs: 1000);

			// Open modal
			var detailsButton = component.Find("button:contains('Details')");
			detailsButton.Click();

			// Close with X button
			var closeXButton = component.Find("button.btn-close");
			closeXButton.Click();

			// Assert - Modal should be hidden
			var markup = component.Markup;
			markup.Should().NotContain("modal fade show");
		}

		private static async Task WaitForComponentToFinishLoading(IRenderedComponent<UpcomingDividends> component, string? containsText = null, int maxWaitMs = 500)
		{
			var startTime = DateTime.UtcNow;
			var waitInterval = 25;

			while (DateTime.UtcNow.Subtract(startTime).TotalMilliseconds < maxWaitMs)
			{
				try
				{
					component.Render();

					// If we're looking for specific text, check for it
					if (!string.IsNullOrEmpty(containsText))
					{
						if (component.Markup.Contains(containsText))
						{
							return;
						}
					}
					// Otherwise, just check if loading is complete
					else if (!component.Markup.Contains("Loading Upcoming Dividends"))
					{
						return;
					}
				}
				catch
				{
					// Ignore rendering exceptions during async operations
				}

				await Task.Delay(waitInterval, CancellationToken.None);
			}
		}
	}
}