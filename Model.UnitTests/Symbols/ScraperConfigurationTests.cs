using AwesomeAssertions;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.Model.UnitTests.Symbols
{
	public class ScraperConfigurationTests
	{
		[Fact]
		public void IsValid_ShouldReturnFalse_WhenUrlIsNull()
		{
			// Arrange
			var config = new ScraperConfiguration
			{
				Url = null,
				Selector = "selector"
			};

			// Act
			var result = config.IsValid;

			// Assert
			result.Should().BeFalse();
		}

		[Fact]
		public void IsValid_ShouldReturnFalse_WhenSelectorIsNull()
		{
			// Arrange
			var config = new ScraperConfiguration
			{
				Url = "url",
				Selector = null
			};

			// Act
			var result = config.IsValid;

			// Assert
			result.Should().BeFalse();
		}

		[Fact]
		public void IsValid_ShouldReturnFalse_WhenUrlIsEmpty()
		{
			// Arrange
			var config = new ScraperConfiguration
			{
				Url = "",
				Selector = "selector"
			};

			// Act
			var result = config.IsValid;

			// Assert
			result.Should().BeFalse();
		}

		[Fact]
		public void IsValid_ShouldReturnFalse_WhenSelectorIsEmpty()
		{
			// Arrange
			var config = new ScraperConfiguration
			{
				Url = "url",
				Selector = ""
			};

			// Act
			var result = config.IsValid;

			// Assert
			result.Should().BeFalse();
		}

		[Fact]
		public void IsValid_ShouldReturnTrue_WhenUrlAndSelectorAreNotNullOrEmpty()
		{
			// Arrange
			var config = new ScraperConfiguration
			{
				Url = "url",
				Selector = "selector"
			};

			// Act
			var result = config.IsValid;

			// Assert
			result.Should().BeTrue();
		}
	}
}
