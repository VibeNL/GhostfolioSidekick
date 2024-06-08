using FluentAssertions;
using GhostfolioSidekick.GhostfolioAPI.API.Mapper;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Configuration;

namespace GhostfolioSidekick.GhostfolioAPI.UnitTests.API.Mapper
{
	public class SymbolMapperTests
	{
		private readonly SymbolMapper symbolMapper;
		private readonly List<Mapping> mappings;

		public SymbolMapperTests()
		{
			mappings =
			[
				new Mapping { MappingType = MappingType.Currency, Source = "USD", Target = "US Dollar" },
				new Mapping { MappingType = MappingType.Symbol, Source = "AAPL", Target = "Apple Inc." }
			];

			symbolMapper = new SymbolMapper(mappings);
		}

		[Fact]
		public void MapCurrency_ShouldReturnMappedCurrency()
		{
			// Arrange
			var sourceCurrency = "USD";

			// Act
			var result = symbolMapper.Map(sourceCurrency);

			// Assert
			result.Should().BeEquivalentTo(new Currency("US Dollar"));
		}

		[Fact]
		public void MapCurrency_ShouldReturnSameCurrency_WhenNoMappingExists()
		{
			// Arrange
			var sourceCurrency = "EUR";

			// Act
			var result = symbolMapper.Map(sourceCurrency);

			// Assert
			result.Should().BeEquivalentTo(new Currency("EUR"));
		}

		[Fact]
		public void MapSymbol_ShouldReturnMappedSymbol()
		{
			// Arrange
			var sourceSymbol = "AAPL";

			// Act
			var result = symbolMapper.MapSymbol(sourceSymbol);

			// Assert
			result.Should().Be("Apple Inc.");
		}

		[Fact]
		public void MapSymbol_ShouldReturnSameSymbol_WhenNoMappingExists()
		{
			// Arrange
			var sourceSymbol = "GOOGL";

			// Act
			var result = symbolMapper.MapSymbol(sourceSymbol);

			// Assert
			result.Should().Be("GOOGL");
		}
	}
}
