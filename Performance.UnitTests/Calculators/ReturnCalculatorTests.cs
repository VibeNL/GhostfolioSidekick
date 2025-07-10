using GhostfolioSidekick.Performance.Calculators;
using AwesomeAssertions;

namespace GhostfolioSidekick.Performance.UnitTests.Calculators
{
    public class ReturnCalculatorTests
    {
        [Theory]
        [InlineData(100, 110, 0.1)]
        [InlineData(100, 90, -0.1)]
        [InlineData(100, 100, 0)]
        [InlineData(0, 100, 0)] // Edge case: zero start value
        public void CalculateSimpleReturn_ShouldReturnCorrectValue(decimal startValue, decimal endValue, decimal expected)
        {
            // Act
            var result = ReturnCalculator.CalculateSimpleReturn(startValue, endValue);

            // Assert
            result.Should().BeApproximately(expected, 0.0001m);
        }

        [Theory]
        [InlineData(100, 110, 365.25, 0.1)]
        [InlineData(100, 121, 730.5, 0.1)] // 2 years, 21% total = ~10% CAGR
        public void CalculateCAGR_ShouldReturnCorrectValue(decimal startValue, decimal endValue, decimal days, decimal expectedApprox)
        {
            // Arrange
            var years = days / 365.25m;

            // Act
            var result = ReturnCalculator.CalculateCAGR(startValue, endValue, years);

            // Assert
            result.Should().BeApproximately(expectedApprox, 0.01m);
        }

        [Fact]
        public void CalculateTimeWeightedReturn_ShouldCompoundReturnsCorrectly()
        {
            // Arrange
            var subPeriodReturns = new[] { 0.05m, 0.03m, -0.02m }; // 5%, 3%, -2%

            // Act
            var result = ReturnCalculator.CalculateTimeWeightedReturn(subPeriodReturns);

            // Assert
            var expected = (1.05m * 1.03m * 0.98m) - 1;
            result.Should().BeApproximately(expected, 0.0001m);
        }

        [Fact]
        public void CalculateTimeWeightedReturn_WithEmptyArray_ShouldReturnZero()
        {
            // Act
            var result = ReturnCalculator.CalculateTimeWeightedReturn([]);

            // Assert
            result.Should().Be(0);
        }

        [Fact]
        public void CalculateRollingReturns_ShouldCalculateCorrectly()
        {
            // Arrange
            var values = new[] { 100m, 105m, 110m, 108m, 115m };
            var windowSize = 2;

            // Act
            var result = ReturnCalculator.CalculateRollingReturns(values, windowSize);

            // Assert
            result.Should().HaveCount(3);
            result[0].Should().BeApproximately(0.1m, 0.0001m); // (110-100)/100
            result[1].Should().BeApproximately(0.08m, 0.0001m); // (108-105)/105
            result[2].Should().BeApproximately(0.045454m, 0.001m); // (115-110)/110
        }

        [Fact]
        public void CalculateCumulativeReturns_ShouldCompoundCorrectly()
        {
            // Arrange
            var periodicReturns = new[] { 0.1m, 0.05m, -0.02m };

            // Act
            var result = ReturnCalculator.CalculateCumulativeReturns(periodicReturns);

            // Assert
            result.Should().HaveCount(3);
            result[0].Should().BeApproximately(0.1m, 0.0001m); // 10%
            result[1].Should().BeApproximately(0.155m, 0.0001m); // (1.1 * 1.05) - 1
            result[2].Should().BeApproximately(0.1319m, 0.0001m); // (1.1 * 1.05 * 0.98) - 1
        }

        [Theory]
        [InlineData(0.21, 730.5, 0.1)] // 21% over 2 years ? 10% annualized
        [InlineData(0.1, 365.25, 0.1)] // 10% over 1 year = 10% annualized
        public void AnnualizeReturn_ShouldCalculateCorrectly(decimal totalReturn, decimal days, decimal expectedApprox)
        {
            // Act
            var result = ReturnCalculator.AnnualizeReturn(totalReturn, days);

            // Assert
            result.Should().BeApproximately(expectedApprox, 0.01m);
        }

        [Fact]
        public void CalculateGeometricMean_ShouldCalculateCorrectly()
        {
            // Arrange
            var returns = new[] { 0.1m, 0.05m, -0.02m };

            // Act
            var result = ReturnCalculator.CalculateGeometricMean(returns);

            // Assert
            var expected = (decimal)Math.Pow((double)(1.1m * 1.05m * 0.98m), 1.0 / 3.0) - 1;
            result.Should().BeApproximately(expected, 0.0001m);
        }

        [Fact]
        public void CalculateRealReturn_ShouldAdjustForInflation()
        {
            // Arrange
            var nominalReturn = 0.07m; // 7%
            var inflationRate = 0.03m; // 3%

            // Act
            var result = ReturnCalculator.CalculateRealReturn(nominalReturn, inflationRate);

            // Assert
            var expected = (1.07m / 1.03m) - 1;
            result.Should().BeApproximately(expected, 0.0001m);
        }

        [Fact]
        public void CalculateExcessReturn_ShouldReturnDifference()
        {
            // Arrange
            var portfolioReturn = 0.12m;
            var benchmarkReturn = 0.08m;

            // Act
            var result = ReturnCalculator.CalculateExcessReturn(portfolioReturn, benchmarkReturn);

            // Assert
            result.Should().Be(0.04m);
        }
    }
}