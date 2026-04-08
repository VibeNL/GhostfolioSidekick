using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using GhostfolioSidekick.PortfolioViewer.WASM.Services;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Services;
using Moq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace GhostfolioSidekick.PortfolioViewer.WASM.UnitTests.Services
{
	public class TaxDetailsServiceTests
	{
		private readonly Mock<IAccountDataService> _accountServiceMock = new();
		private readonly Mock<ITransactionService> _transactionServiceMock = new();
		private readonly Mock<IServerConfigurationService> _configServiceMock = new();
		private readonly TaxDetailsService _service;

		public TaxDetailsServiceTests()
		{
			_service = new TaxDetailsService(_accountServiceMock.Object, _transactionServiceMock.Object, _configServiceMock.Object);
		}

		[Fact]
		public async Task GetAvailableYearsAsync_ReturnsYears()
		{
			_transactionServiceMock.Setup(x => x.GetAvailableYearsAsync()).ReturnsAsync(new List<int> { 2022, 2023 });
			var years = await _service.GetAvailableYearsAsync();
			Assert.Equal(new List<int> { 2022, 2023 }, years);
		}

		[Fact]
		public async Task GetTaxAccountDetailsAsync_ComputesSymbolRows()
		{
			var account = new TaxAccountDisplayModel
			{
				StartHoldings = new List<TaxHoldingDisplayModel> { new() { Symbol = "A", Quantity = 1, Value = 100 } },
				EndHoldings = new List<TaxHoldingDisplayModel> { new() { Symbol = "B", Quantity = 2, Value = 200 } }
			};
			_accountServiceMock.Setup(x => x.GetTaxAccountDetailsAsync(2023)).ReturnsAsync(new List<TaxAccountDisplayModel> { account });
			var result = await _service.GetTaxAccountDetailsAsync(2023);
			Assert.NotNull(result);
			Assert.Equal(2, result![0].SymbolRows.Count);
			Assert.Equal(100, result[0].TotalStartValue);
			Assert.Equal(200, result[0].TotalEndValue);
		}

		[Theory]
		[InlineData("EUR", 123.45, "€123.45")]
		[InlineData("USD", 99.99, "$99.99")]
		[InlineData("GBP", 10, "£10.00")]
		[InlineData("JPY", 5, "JPY 5.00")]
		public void FormatCurrency_UsesSymbol(string currency, decimal amount, string expected)
		{
			_configServiceMock.Setup(x => x.PrimaryCurrency).Returns(new GhostfolioSidekick.Model.Currency { Symbol = currency });
			var service = new TaxDetailsService(_accountServiceMock.Object, _transactionServiceMock.Object, _configServiceMock.Object);
			var formatted = service.FormatCurrency(amount);
			Assert.Equal(expected, formatted);
		}

		[Fact]
		public void HasValues_ReturnsTrue_WhenValuesPresent()
		{
			var account = new TaxAccountDisplayModel { StartValue = 1 };
			Assert.True(_service.HasValues(account));
			account = new TaxAccountDisplayModel { EndValue = 1 };
			Assert.True(_service.HasValues(account));
			account = new TaxAccountDisplayModel { StartCashBalance = 1 };
			Assert.True(_service.HasValues(account));
			account = new TaxAccountDisplayModel { EndCashBalance = 1 };
			Assert.True(_service.HasValues(account));
			account = new TaxAccountDisplayModel { StartHoldings = new List<TaxHoldingDisplayModel> { new() } };
			Assert.True(_service.HasValues(account));
			account = new TaxAccountDisplayModel { EndHoldings = new List<TaxHoldingDisplayModel> { new() } };
			Assert.True(_service.HasValues(account));
			account = new TaxAccountDisplayModel { Holdings = new List<TaxHoldingDisplayModel> { new() } };
			Assert.True(_service.HasValues(account));
			account = new TaxAccountDisplayModel { Transactions = new List<TaxTransactionDisplayModel> { new() { Type = "Buy", Amount = 1 } } };
			Assert.True(_service.HasValues(account));
			account = new TaxAccountDisplayModel { Dividends = new List<TaxDividendDisplayModel> { new() } };
			Assert.True(_service.HasValues(account));
			account = new TaxAccountDisplayModel { RealizedGainsLosses = new List<TaxGainLossDisplayModel> { new() } };
			Assert.True(_service.HasValues(account));
		}

		[Fact]
		public void HasValues_ReturnsFalse_WhenNoValues()
		{
			var account = new TaxAccountDisplayModel();
			Assert.False(_service.HasValues(account));
		}
	}
}
