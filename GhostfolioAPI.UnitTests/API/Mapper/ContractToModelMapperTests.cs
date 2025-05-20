using FluentAssertions;
using GhostfolioSidekick.GhostfolioAPI.API.Mapper;
using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.GhostfolioAPI.UnitTests.API.Mapper
{
	public class ContractToModelMapperTests
	{
		[Fact]
		public void MapPlatform_ShouldMapCorrectly()
		{
			// Arrange
			var rawPlatform = new Contract.Platform
			{
				Name = "Test Platform",
				Url = "http://testplatform.com",
				Id = Guid.NewGuid().ToString()
			};

			// Act
			var result = ContractToModelMapper.MapPlatform(rawPlatform);

			// Assert
			result.Name.Should().Be("Test Platform");
			result.Url.Should().Be("http://testplatform.com");
		}

		[Fact]
		public void MapAccount_ShouldMapCorrectly()
		{
			// Arrange
			var rawAccount = new Contract.Account
			{
				Name = "Test Account",
				Comment = "Test Comment",
				Currency = "USD",
				Balance = 1000m,
				Id = Guid.NewGuid().ToString()
			};
			var rawPlatform = new Contract.Platform
			{
				Name = "Test Platform",
				Url = "http://testplatform.com",
				Id = Guid.NewGuid().ToString()
			};

			// Act
			var result = ContractToModelMapper.MapAccount(rawAccount, rawPlatform);

			// Assert
			result.Name.Should().Be("Test Account");
			result.Comment.Should().Be("Test Comment");
			result.Platform.Should().NotBeNull();
			result.Platform!.Name.Should().Be("Test Platform");
			result.Balance.Should().HaveCount(1);
			result.Balance.First().Money.Amount.Should().Be(1000m);
		}

		[Fact]
		public void MapSymbolProfile_ShouldMapCorrectly()
		{
			// Arrange
			var rawSymbolProfile = new Contract.SymbolProfile
			{
				Symbol = "AAPL",
				Name = "Apple Inc.",
				Currency = "USD",
				DataSource = "Yahoo",
				AssetClass = "EQUITY",
				AssetSubClass = "STOCK",
				ISIN = "US0378331005",
				Comment = "Test Comment",
				Countries =
				[
					new Contract.Country { Name = "United States", Code = "US", Continent = "North America", Weight = 100m }
				],
				Sectors =
				[
					new Contract.Sector { Name = "Technology", Weight = 100m }
				]
			};

			// Act
			var result = ContractToModelMapper.MapSymbolProfile(rawSymbolProfile);

			// Assert
			result.Symbol.Should().Be("AAPL");
			result.Name.Should().Be("Apple Inc.");
			result.Currency.Symbol.Should().Be("USD");
			result.DataSource.Should().Be("GHOSTFOLIO_Yahoo");
			result.AssetClass.Should().Be(AssetClass.Equity);
			result.AssetSubClass.Should().Be(AssetSubClass.Stock);
			result.ISIN.Should().Be("US0378331005");
			result.Comment.Should().Be("Test Comment");
			result.CountryWeight.Should().HaveCount(1);
			result.SectorWeights.Should().HaveCount(1);
		}
	}
}
