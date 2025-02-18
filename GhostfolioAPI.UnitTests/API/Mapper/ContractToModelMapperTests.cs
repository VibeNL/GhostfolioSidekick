using Shouldly;
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
			result.Name.ShouldBe("Test Platform");
			result.Url.ShouldBe("http://testplatform.com");
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
			result.Name.ShouldBe("Test Account");
			result.Comment.ShouldBe("Test Comment");
			result.Platform.ShouldNotBeNull();
			result.Platform!.Name.ShouldBe("Test Platform");
			result.Balance.ShouldHaveSingleItem();
			result.Balance.First().Money.Amount.ShouldBe(1000m);
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
				Countries = new[]
				{
					new Contract.Country { Name = "United States", Code = "US", Continent = "North America", Weight = 100m }
				},
				Sectors = new[]
				{
					new Contract.Sector { Name = "Technology", Weight = 100m }
				}
			};

			// Act
			var result = ContractToModelMapper.MapSymbolProfile(rawSymbolProfile);

			// Assert
			result.Symbol.ShouldBe("AAPL");
			result.Name.ShouldBe("Apple Inc.");
			result.Currency.Symbol.ShouldBe("USD");
			result.DataSource.ShouldBe("GHOSTFOLIO_Yahoo");
			result.AssetClass.ShouldBe(AssetClass.Equity);
			result.AssetSubClass.ShouldBe(AssetSubClass.Stock);
			result.ISIN.ShouldBe("US0378331005");
			result.Comment.ShouldBe("Test Comment");
			result.CountryWeight.ShouldHaveSingleItem();
			result.SectorWeights.ShouldHaveSingleItem();
		}
	}
}
