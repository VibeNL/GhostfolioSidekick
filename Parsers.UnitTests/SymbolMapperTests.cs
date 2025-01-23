using GhostfolioSidekick.Parsers;
using FluentAssertions;
using GhostfolioSidekick.Configuration;

namespace GhostfolioSidekick.Tests.Parsers
{
	public class SymbolMapperTests
    {
        private readonly List<Mapping> mappings = new List<Mapping>
        {
            new Mapping { MappingType = MappingType.Currency, Source = "USD", Target = "US Dollar" },
            new Mapping { MappingType = MappingType.Symbol, Source = "AAPL", Target = "Apple Inc." }
        };

        [Fact]
        public void Map_ShouldReturnMappedCurrency()
        {
            // Arrange
            var symbolMapper = new SymbolMapper(mappings);

            // Act
            var result = symbolMapper.Map("USD");

            // Assert
            result.Symbol.Should().Be("US Dollar");
        }

        [Fact]
        public void Map_ShouldReturnOriginalCurrencyIfNotMapped()
        {
            // Arrange
            var symbolMapper = new SymbolMapper(mappings);

            // Act
            var result = symbolMapper.Map("EUR");

            // Assert
            result.Symbol.Should().Be("EUR");
        }

        [Fact]
        public void MapSymbol_ShouldReturnMappedSymbol()
        {
            // Arrange
            var symbolMapper = new SymbolMapper(mappings);

            // Act
            var result = symbolMapper.MapSymbol("AAPL");

            // Assert
            result.Should().Be("Apple Inc.");
        }

        [Fact]
        public void MapSymbol_ShouldReturnOriginalSymbolIfNotMapped()
        {
            // Arrange
            var symbolMapper = new SymbolMapper(mappings);

            // Act
            var result = symbolMapper.MapSymbol("MSFT");

            // Assert
            result.Should().Be("MSFT");
        }
    }
}
