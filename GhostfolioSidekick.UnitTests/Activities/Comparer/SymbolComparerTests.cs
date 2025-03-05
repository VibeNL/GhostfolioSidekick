using GhostfolioSidekick.Activities.Comparer;
using GhostfolioSidekick.Model.Symbols;
using FluentAssertions;

namespace GhostfolioSidekick.UnitTests.Activities.Comparer
{
    public class SymbolComparerTests
    {
        [Fact]
        public void Equals_ShouldReturnTrueForEqualSymbols()
        {
            // Arrange
            var symbol1 = new SymbolProfile
            {
                Symbol = "SYM1",
                DataSource = "DataSource1",
                AssetClass = "AssetClass1",
                AssetSubClass = "AssetSubClass1",
                Currency = new Currency { Symbol = "USD" }
            };

            var symbol2 = new SymbolProfile
            {
                Symbol = "SYM1",
                DataSource = "DataSource1",
                AssetClass = "AssetClass1",
                AssetSubClass = "AssetSubClass1",
                Currency = new Currency { Symbol = "USD" }
            };

            var comparer = new SymbolComparer();

            // Act
            var result = comparer.Equals(symbol1, symbol2);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Equals_ShouldReturnFalseForUnequalSymbols()
        {
            // Arrange
            var symbol1 = new SymbolProfile
            {
                Symbol = "SYM1",
                DataSource = "DataSource1",
                AssetClass = "AssetClass1",
                AssetSubClass = "AssetSubClass1",
                Currency = new Currency { Symbol = "USD" }
            };

            var symbol2 = new SymbolProfile
            {
                Symbol = "SYM2",
                DataSource = "DataSource2",
                AssetClass = "AssetClass2",
                AssetSubClass = "AssetSubClass2",
                Currency = new Currency { Symbol = "EUR" }
            };

            var comparer = new SymbolComparer();

            // Act
            var result = comparer.Equals(symbol1, symbol2);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void GetHashCode_ShouldReturnSameHashCodeForEqualSymbols()
        {
            // Arrange
            var symbol1 = new SymbolProfile
            {
                Symbol = "SYM1",
                DataSource = "DataSource1",
                AssetClass = "AssetClass1",
                AssetSubClass = "AssetSubClass1",
                Currency = new Currency { Symbol = "USD" }
            };

            var symbol2 = new SymbolProfile
            {
                Symbol = "SYM1",
                DataSource = "DataSource1",
                AssetClass = "AssetClass1",
                AssetSubClass = "AssetSubClass1",
                Currency = new Currency { Symbol = "USD" }
            };

            var comparer = new SymbolComparer();

            // Act
            var hashCode1 = comparer.GetHashCode(symbol1);
            var hashCode2 = comparer.GetHashCode(symbol2);

            // Assert
            hashCode1.Should().Be(hashCode2);
        }
    }
}
