using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using GhostfolioSidekick.Model;
using Xunit;
using System.Collections.Generic;

namespace PortfolioViewer.WASM.Data.UnitTests.Models
{
    public class HoldingDisplayModelTests
    {
        [Fact]
        public void Constructor_InitializesDefaults()
        {
            var model = new HoldingDisplayModel
            {
                CurrentValue = new Money(Currency.USD, 100),
                AveragePrice = new Money(Currency.USD, 10),
                CurrentPrice = new Money(Currency.USD, 12),
                GainLoss = new Money(Currency.USD, 2)
            };
            Assert.NotNull(model.Symbols);
            Assert.Equal(string.Empty, model.Name);
            Assert.Equal(0, model.Quantity);
            Assert.Equal(0, model.GainLossPercentage);
            Assert.Equal(0, model.Weight);
            Assert.Equal(string.Empty, model.Sector);
            Assert.Equal(string.Empty, model.AssetClass);
            Assert.Equal("USD", model.Currency);
        }

        [Fact]
        public void CanSetAndGetProperties()
        {
            var model = new HoldingDisplayModel
            {
                Symbols = new List<string> { "AAPL", "APC" },
                Name = "Apple Inc.",
                CurrentValue = new Money(Currency.USD, 100),
                Quantity = 5,
                AveragePrice = new Money(Currency.USD, 10),
                CurrentPrice = new Money(Currency.USD, 12),
                GainLoss = new Money(Currency.USD, 2),
                GainLossPercentage = 20,
                Weight = 50,
                Sector = "Tech",
                AssetClass = "Equity",
                Currency = "EUR"
            };
            Assert.Equal(new List<string> { "AAPL", "APC" }, model.Symbols);
            Assert.Equal("Apple Inc.", model.Name);
            Assert.Equal(5, model.Quantity);
            Assert.Equal(20, model.GainLossPercentage);
            Assert.Equal(50, model.Weight);
            Assert.Equal("Tech", model.Sector);
            Assert.Equal("Equity", model.AssetClass);
            Assert.Equal("EUR", model.Currency);
        }

        [Fact]
        public void ToString_ReturnsExpectedFormat()
        {
            var model = new HoldingDisplayModel
            {
                Symbols = new List<string> { "AAPL" },
                Name = "Apple Inc.",
                CurrentValue = new Money(Currency.USD, 100),
                Quantity = 5,
                AveragePrice = new Money(Currency.USD, 10),
                CurrentPrice = new Money(Currency.USD, 12),
                GainLoss = new Money(Currency.USD, 2),
                GainLossPercentage = 20,
                Weight = 50,
                Sector = "Tech",
                AssetClass = "Equity",
                Currency = "USD"
            };
            var str = model.ToString();
            Assert.Contains("Apple Inc.", str);
            Assert.Contains("AAPL", str);
            Assert.Contains("Current Value", str);
            Assert.Contains("Gain/Loss Percentage: 20%", str);
        }
    }
}
