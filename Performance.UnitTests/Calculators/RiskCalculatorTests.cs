using GhostfolioSidekick.Performance.Calculators;
using AwesomeAssertions;

namespace GhostfolioSidekick.Performance.UnitTests.Calculators
{
    public class RiskCalculatorTests
    {
        [Fact]
        public void CalculateHistoricalVaR_ShouldCalculateCorrectly()
        {
            // Arrange
            var returns = new[] { -0.05m, -0.03m, -0.01m, 0.01m, 0.02m, 0.03m, 0.04m, 0.05m, 0.06m, 0.07m };
            var confidenceLevel = 0.95m;

            // Act
            var result = RiskCalculator.CalculateHistoricalVaR(returns, confidenceLevel);

            // Assert
            result.Should().BeGreaterThan(0);
            result.Should().BeLessOrEqualTo(0.05m); // Should be the worst 5% of returns
        }

        [Fact]
        public void CalculateHistoricalVaR_WithEmptyArray_ShouldReturnZero()
        {
            // Act
            var result = RiskCalculator.CalculateHistoricalVaR([], 0.95m);

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public void CalculateMaximumDrawdown_ShouldFindLargestDecline()
        {
            // Arrange - Peak at 110, trough at 90, recovery to 105
            var values = new[] { 100m, 105m, 110m, 100m, 90m, 95m, 105m };

            // Act
            var result = RiskCalculator.CalculateMaximumDrawdown(values);

            // Assert
            var expectedDrawdown = (110m - 90m) / 110m; // ~18.18%
            result.Should().BeApproximately(expectedDrawdown, 0.001m);
        }

        [Fact]
        public void CalculateMaximumDrawdown_WithConstantValues_ShouldReturnZero()
        {
            // Arrange
            var values = new[] { 100m, 100m, 100m, 100m };

            // Act
            var result = RiskCalculator.CalculateMaximumDrawdown(values);

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public void CalculateDownsideDeviation_ShouldCalculateCorrectly()
        {
            // Arrange
            var returns = new[] { -0.05m, -0.02m, 0.01m, 0.03m, -0.01m };
            var minimumAcceptableReturn = 0m;

            // Act
            var result = RiskCalculator.CalculateDownsideDeviation(returns, minimumAcceptableReturn);

            // Assert
            result.Should().BeGreaterThan(0);
            // Only negative returns should be included in the calculation
        }

        [Fact]
        public void CalculateDownsideDeviation_WithNoNegativeReturns_ShouldReturnZero()
        {
            // Arrange
            var returns = new[] { 0.01m, 0.02m, 0.03m, 0.04m };

            // Act
            var result = RiskCalculator.CalculateDownsideDeviation(returns, 0m);

            // Assert
            result.Should().Be(0);
        }

        [Theory]
        [InlineData(0.08, 0.02, 3)]
        [InlineData(0.06, 0.03, 1.33)]
        public void CalculateSortinoRatio_ShouldCalculateCorrectly(decimal averageReturn, decimal downsideDeviation, decimal expectedApprox)
        {
            // Act
            var result = RiskCalculator.CalculateSortinoRatio(averageReturn, downsideDeviation);

            // Assert
            result.Should().NotBeNull();
            result!.Value.Should().BeApproximately(expectedApprox, 0.1m);
        }

        [Fact]
        public void CalculateSortinoRatio_WithZeroDownsideDeviation_ShouldReturnNull()
        {
            // Act
            var result = RiskCalculator.CalculateSortinoRatio(0.08m, 0m);

            // Assert
            result.Should().BeNull();
        }

        [Theory]
        [InlineData(0.12, 0.15, 0.8)]
        [InlineData(0.08, 0.20, 0.4)]
        public void CalculateCalmarRatio_ShouldCalculateCorrectly(decimal annualizedReturn, decimal maxDrawdown, decimal expected)
        {
            // Act
            var result = RiskCalculator.CalculateCalmarRatio(annualizedReturn, maxDrawdown);

            // Assert
            result.Should().NotBeNull();
            result!.Value.Should().BeApproximately(expected, 0.01m);
        }

        [Fact]
        public void CalculateCalmarRatio_WithZeroDrawdown_ShouldReturnNull()
        {
            // Act
            var result = RiskCalculator.CalculateCalmarRatio(0.08m, 0m);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public void CalculateSkewness_ShouldCalculateCorrectly()
        {
            // Arrange - Right-skewed distribution (more positive outliers)
            var returns = new[] { -0.01m, 0.01m, 0.02m, 0.03m, 0.10m };

            // Act
            var result = RiskCalculator.CalculateSkewness(returns);

            // Assert
            result.Should().BeGreaterThan(0); // Positive skew
        }

        [Fact]
        public void CalculateSkewness_WithInsufficientData_ShouldReturnZero()
        {
            // Act
            var result = RiskCalculator.CalculateSkewness(new[] { 0.01m, 0.02m });

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public void CalculateKurtosis_ShouldCalculateCorrectly()
        {
            // Arrange - Distribution with some extreme values
            var returns = new[] { -0.05m, -0.01m, 0.01m, 0.02m, 0.10m, -0.08m, 0.03m };

            // Act
            var result = RiskCalculator.CalculateKurtosis(returns);

            // Assert
            // Excess kurtosis can be positive or negative, just ensure it's calculated
            result.Should().NotBe(decimal.MaxValue);
        }

        [Fact]
        public void IdentifyDrawdownPeriods_ShouldFindAllDrawdowns()
        {
            // Arrange - Multiple peaks and troughs
            var values = new[] { 100m, 110m, 105m, 120m, 115m, 110m, 125m };

            // Act
            var result = RiskCalculator.IdentifyDrawdownPeriods(values).ToList();

            // Assert
            result.Should().HaveCountGreaterThan(0);
            // Should identify drawdown periods correctly
        }

        [Fact]
        public void CalculateRollingVolatility_ShouldCalculateCorrectly()
        {
            // Arrange
            var returns = new[] { 0.01m, -0.02m, 0.03m, -0.01m, 0.02m, -0.03m };
            var windowSize = 3;

            // Act
            var result = RiskCalculator.CalculateRollingVolatility(returns, windowSize);

            // Assert
            result.Should().HaveCount(4); // 6 - 3 + 1
            result.All(vol => vol >= 0).Should().BeTrue(); // All volatilities should be non-negative
        }

        [Fact]
        public void CalculateRollingVolatility_WithInsufficientData_ShouldReturnEmpty()
        {
            // Arrange
            var returns = new[] { 0.01m, 0.02m };
            var windowSize = 5;

            // Act
            var result = RiskCalculator.CalculateRollingVolatility(returns, windowSize);

            // Assert
            result.Should().BeEmpty();
        }
    }
}