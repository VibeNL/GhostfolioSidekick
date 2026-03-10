using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using System;
using System.Collections.Generic;
using Xunit;

namespace PortfolioViewer.WASM.Data.UnitTests.Models
{
	public class TaxAccountDisplayModelTests
	{
		[Fact]
		public void Properties_Should_Set_And_Get_Correctly()
		{
			var startHoldings = new List<TaxHoldingDisplayModel>
			{
				new TaxHoldingDisplayModel { Symbol = "AAPL", Quantity = 10, Value = 1500, AcquisitionDate = new DateTime(2023, 1, 1) }
			};
			var endHoldings = new List<TaxHoldingDisplayModel>
			{
				new TaxHoldingDisplayModel { Symbol = "AAPL", Quantity = 12, Value = 1800, AcquisitionDate = new DateTime(2023, 12, 31) }
			};

			var model = new TaxAccountDisplayModel
			{
				Name = "Test Account",
				AccountType = "Brokerage",
				StartHoldings = startHoldings,
				EndHoldings = endHoldings,
				StartValue = 1500,
				EndValue = 1800,
				StartCashBalance = 500,
				EndCashBalance = 600
			};

			Assert.Equal("Test Account", model.Name);
			Assert.Equal("Brokerage", model.AccountType);
			Assert.Equal(startHoldings, model.StartHoldings);
			Assert.Equal(endHoldings, model.EndHoldings);
			Assert.Equal(1500, model.StartValue);
			Assert.Equal(1800, model.EndValue);
			Assert.Equal(500, model.StartCashBalance);
			Assert.Equal(600, model.EndCashBalance);
		}

		[Fact]
		public void Default_Collections_Should_Not_Be_Null()
		{
			var model = new TaxAccountDisplayModel();
			Assert.NotNull(model.Holdings);
			Assert.NotNull(model.StartHoldings);
			Assert.NotNull(model.EndHoldings);
			Assert.NotNull(model.Transactions);
			Assert.NotNull(model.Dividends);
			Assert.NotNull(model.RealizedGainsLosses);
		}
	}
}
