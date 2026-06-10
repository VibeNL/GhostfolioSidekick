using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using GhostfolioSidekick.Model;
using Xunit;

namespace PortfolioViewer.WASM.Data.UnitTests.Models
{
    public class TransactionRowTests
    {
        [Fact]
        public void Constructor_InitializesPropertiesToDefaults()
        {
            var row = new TransactionRow();
            Assert.Equal(default(DateTime), row.Date);
            Assert.Equal(string.Empty, row.Description);
            Assert.Null(row.Amount);
            Assert.Equal(string.Empty, row.Type);
        }

        [Fact]
        public void CanSetAndGetProperties()
        {
            var money = new Money(Currency.USD, 123.45m);
            var date = new DateTime(2024, 5, 1);
            var row = new TransactionRow
            {
                Date = date,
                Description = "Test Desc",
                Amount = money,
                Type = "Buy"
            };
            Assert.Equal(date, row.Date);
            Assert.Equal("Test Desc", row.Description);
            Assert.Equal(money, row.Amount);
            Assert.Equal("Buy", row.Type);
        }
    }
}
