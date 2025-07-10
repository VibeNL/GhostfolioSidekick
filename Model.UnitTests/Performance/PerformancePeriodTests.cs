using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Performance;
using AwesomeAssertions;

namespace GhostfolioSidekick.Model.UnitTests.Performance
{
    public class PerformancePeriodTests
    {
        [Fact]
        public void Constructor_WithValidDates_ShouldCreatePeriod()
        {
            // Arrange
            var startDate = new DateOnly(2024, 1, 1);
            var endDate = new DateOnly(2024, 12, 31);
            var periodType = PerformancePeriodType.Yearly;
            var label = "2024";

            // Act
            var period = new PerformancePeriod(startDate, endDate, periodType, label);

            // Assert
            period.StartDate.Should().Be(startDate);
            period.EndDate.Should().Be(endDate);
            period.PeriodType.Should().Be(periodType);
            period.Label.Should().Be(label);
        }

        [Fact]
        public void Constructor_WithStartDateAfterEndDate_ShouldThrowException()
        {
            // Arrange
            var startDate = new DateOnly(2024, 12, 31);
            var endDate = new DateOnly(2024, 1, 1);

            // Act & Assert
            Assert.Throws<ArgumentException>(() => 
                new PerformancePeriod(startDate, endDate, PerformancePeriodType.Custom));
        }

        [Fact]
        public void DaysInPeriod_ShouldCalculateCorrectly()
        {
            // Arrange
            var startDate = new DateOnly(2024, 1, 1);
            var endDate = new DateOnly(2024, 1, 31);
            var period = new PerformancePeriod(startDate, endDate, PerformancePeriodType.Monthly);

            // Act
            var daysInPeriod = period.DaysInPeriod;

            // Assert
            daysInPeriod.Should().Be(31);
        }

        [Fact]
        public void IsCurrentPeriod_WhenEndDateIsToday_ShouldReturnTrue()
        {
            // Arrange
            var today = DateOnly.FromDateTime(DateTime.Today);
            var period = new PerformancePeriod(today.AddDays(-30), today, PerformancePeriodType.Custom);

            // Act
            var isCurrentPeriod = period.IsCurrentPeriod;

            // Assert
            isCurrentPeriod.Should().BeTrue();
        }

        [Fact]
        public void IsCurrentPeriod_WhenEndDateIsInPast_ShouldReturnFalse()
        {
            // Arrange
            var yesterday = DateOnly.FromDateTime(DateTime.Today.AddDays(-1));
            var period = new PerformancePeriod(yesterday.AddDays(-30), yesterday, PerformancePeriodType.Custom);

            // Act
            var isCurrentPeriod = period.IsCurrentPeriod;

            // Assert
            isCurrentPeriod.Should().BeFalse();
        }

        [Fact]
        public void ForYear_ShouldCreateYearlyPeriod()
        {
            // Arrange
            var year = 2024;

            // Act
            var period = PerformancePeriod.ForYear(year);

            // Assert
            period.StartDate.Should().Be(new DateOnly(2024, 1, 1));
            period.EndDate.Should().Be(new DateOnly(2024, 12, 31));
            period.PeriodType.Should().Be(PerformancePeriodType.Yearly);
            period.Label.Should().Be("2024");
        }

        [Fact]
        public void ForMonth_ShouldCreateMonthlyPeriod()
        {
            // Arrange
            var year = 2024;
            var month = 2; // February

            // Act
            var period = PerformancePeriod.ForMonth(year, month);

            // Assert
            period.StartDate.Should().Be(new DateOnly(2024, 2, 1));
            period.EndDate.Should().Be(new DateOnly(2024, 2, 29)); // 2024 is a leap year
            period.PeriodType.Should().Be(PerformancePeriodType.Monthly);
            period.Label.Should().Be("2024-02");
        }

        [Fact]
        public void LastDays_ShouldCreateCorrectPeriod()
        {
            // Arrange
            var days = 30;
            var today = DateOnly.FromDateTime(DateTime.Today);

            // Act
            var period = PerformancePeriod.LastDays(days);

            // Assert
            period.EndDate.Should().Be(today);
            period.StartDate.Should().Be(today.AddDays(-29)); // 30 days inclusive
            period.PeriodType.Should().Be(PerformancePeriodType.Custom);
            period.Label.Should().Be("Last 30 days");
        }

        [Fact]
        public void SinceInception_ShouldCreateCorrectPeriod()
        {
            // Arrange
            var inceptionDate = new DateOnly(2020, 1, 1);
            var today = DateOnly.FromDateTime(DateTime.Today);

            // Act
            var period = PerformancePeriod.SinceInception(inceptionDate);

            // Assert
            period.StartDate.Should().Be(inceptionDate);
            period.EndDate.Should().Be(today);
            period.PeriodType.Should().Be(PerformancePeriodType.SinceInception);
            period.Label.Should().Be("Since Inception");
        }

        [Fact]
        public void Custom_ShouldCreateCustomPeriod()
        {
            // Arrange
            var startDate = new DateOnly(2024, 3, 15);
            var endDate = new DateOnly(2024, 6, 15);
            var label = "Q2 Period";

            // Act
            var period = PerformancePeriod.Custom(startDate, endDate, label);

            // Assert
            period.StartDate.Should().Be(startDate);
            period.EndDate.Should().Be(endDate);
            period.PeriodType.Should().Be(PerformancePeriodType.Custom);
            period.Label.Should().Be(label);
        }

        [Fact]
        public void ToString_WithLabel_ShouldReturnLabel()
        {
            // Arrange
            var period = new PerformancePeriod(
                new DateOnly(2024, 1, 1),
                new DateOnly(2024, 12, 31),
                PerformancePeriodType.Yearly,
                "Year 2024");

            // Act
            var result = period.ToString();

            // Assert
            result.Should().Be("Year 2024");
        }

        [Fact]
        public void ToString_WithoutLabel_ShouldReturnDateRange()
        {
            // Arrange
            var period = new PerformancePeriod(
                new DateOnly(2024, 1, 1),
                new DateOnly(2024, 12, 31),
                PerformancePeriodType.Yearly);

            // Act
            var result = period.ToString();

            // Assert
            result.Should().Be("2024-01-01 to 2024-12-31");
        }
    }
}