using GhostfolioSidekick.Activities.Comparer;
using GhostfolioSidekick.Model;
using FluentAssertions;

namespace GhostfolioSidekick.UnitTests.Activities.Comparer
{
    public class HoldingComparerTests
    {
        [Fact]
        public void Equals_ShouldReturnTrueForEqualHoldings()
        {
            // Arrange
            var holding1 = new Holding
            {
                SymbolProfiles = new List<SymbolProfile>
                {
                    new SymbolProfile { Symbol = "SYM1" }
                }
            };

            var holding2 = new Holding
            {
                SymbolProfiles = new List<SymbolProfile>
                {
                    new SymbolProfile { Symbol = "SYM1" }
                }
            };

            var comparer = new HoldingComparer();

            // Act
            var result = comparer.Equals(holding1, holding2);

            // Assert
            result.Should().BeTrue();
        }

        [Fact]
        public void Equals_ShouldReturnFalseForUnequalHoldings()
        {
            // Arrange
            var holding1 = new Holding
            {
                SymbolProfiles = new List<SymbolProfile>
                {
                    new SymbolProfile { Symbol = "SYM1" }
                }
            };

            var holding2 = new Holding
            {
                SymbolProfiles = new List<SymbolProfile>
                {
                    new SymbolProfile { Symbol = "SYM2" }
                }
            };

            var comparer = new HoldingComparer();

            // Act
            var result = comparer.Equals(holding1, holding2);

            // Assert
            result.Should().BeFalse();
        }

        [Fact]
        public void GetHashCode_ShouldReturnSameHashCodeForEqualHoldings()
        {
            // Arrange
            var holding1 = new Holding
            {
                SymbolProfiles = new List<SymbolProfile>
                {
                    new SymbolProfile { Symbol = "SYM1" }
                }
            };

            var holding2 = new Holding
            {
                SymbolProfiles = new List<SymbolProfile>
                {
                    new SymbolProfile { Symbol = "SYM1" }
                }
            };

            var comparer = new HoldingComparer();

            // Act
            var hashCode1 = comparer.GetHashCode(holding1);
            var hashCode2 = comparer.GetHashCode(holding2);

            // Assert
            hashCode1.Should().Be(hashCode2);
        }
    }
}
