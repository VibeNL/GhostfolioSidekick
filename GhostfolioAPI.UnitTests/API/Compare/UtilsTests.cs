using GhostfolioSidekick.GhostfolioAPI.API.Compare;
using GhostfolioSidekick.GhostfolioAPI.Contract;

namespace GhostfolioSidekick.GhostfolioAPI.UnitTests.API.Compare
{
	public class UtilsTests
	{
		[Fact]
		public void IsGeneratedSymbol_ShouldReturnTrue_WhenSymbolIsGuidAndDataSourceIsManual()
		{
			// Arrange
			var symbolProfile = new SymbolProfile
			{
				Symbol = "123e4567-e89b-12d3-a456-426614174000",
				DataSource = Model.Symbols.Datasource.MANUAL,
				AssetClass = "Stock",
				Countries = [new() { Code = "US", Name = "US", Continent = "US", Weight = 1 }],
				Currency = "USD",
				Name = "Apple Inc.",
				Sectors = [new() { Name = "Technology", Weight = 1 }]
			};

			// Act
			var result = Utils.IsGeneratedSymbol(symbolProfile);

			// Assert
			Assert.True(result);
		}

		[Fact]

		public void IsGeneratedSymbol_ShouldReturnFalse_WhenSymbolIsNotGuid()
		{
			// Arrange
			var symbolProfile = new SymbolProfile
			{
				Symbol = "AAPL",
				DataSource = Model.Symbols.Datasource.MANUAL,
				AssetClass = "Stock",
				Countries = [new() { Code = "US", Name = "US", Continent = "US", Weight = 1 }],
				Currency = "USD",
				Name = "Apple Inc.",
				Sectors = [new() { Name = "Technology", Weight = 1 }]
			};

			// Act
			var result = Utils.IsGeneratedSymbol(symbolProfile);

			// Assert
			Assert.False(result);
		}

		[Fact]
		public void IsGeneratedSymbol_ShouldReturnFalse_WhenDataSourceIsNotManual()
		{
			// Arrange
			var symbolProfile = new SymbolProfile
			{
				Symbol = "123e4567-e89b-12d3-a456-426614174000",
				DataSource = "AUTOMATIC",
				AssetClass = "Stock",
				Countries = [new() { Code = "US", Name = "US", Continent = "US", Weight = 1 }],
				Currency = "USD",
				Name = "Apple Inc.",
				Sectors = [new() { Name = "Technology", Weight = 1 }]
			};

			// Act
			var result = Utils.IsGeneratedSymbol(symbolProfile);

			// Assert
			Assert.False(result);
		}

		[Fact]
		public void IsGeneratedSymbol_ShouldReturnFalse_WhenSymbolIsNotGuidAndDataSourceIsNotManual()
		{
			// Arrange
			var symbolProfile = new SymbolProfile
			{
				Symbol = "AAPL",
				DataSource = "AUTOMATIC",
				AssetClass = "Stock",
				Countries = [new() { Code = "US", Name = "US", Continent = "US", Weight = 1 }],
				Currency = "USD",
				Name = "Apple Inc.",
				Sectors = [new() { Name = "Technology", Weight = 1 }]
			};

			// Act
			var result = Utils.IsGeneratedSymbol(symbolProfile);

			// Assert
			Assert.False(result);
		}
	}
}
