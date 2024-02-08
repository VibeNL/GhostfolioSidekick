using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.GhostfolioAPI.API.Mapper;

namespace GhostfolioSidekick.GhostfolioAPI.UnitTests.API
{
	public class ContractToModelMapperTests
	{
		[Fact]
		public void MapPlatform_ShouldReturnCorrectPlatform()
		{
			// Arrange
			var rawPlatform = new Fixture().Create<Contract.Platform>();

			// Act
			var result = ContractToModelMapper.MapPlatform(rawPlatform);

			// Assert
			result.Should().BeEquivalentTo(rawPlatform, options => options.ExcludingMissingMembers());
		}

		[Fact]
		public void MapAccount_ShouldReturnCorrectAccount()
		{
			// Arrange
			var rawAccount = new Fixture().Create<Contract.Account>();
			var platform = new Fixture().Create<Model.Accounts.Platform>();

			// Act
			var result = ContractToModelMapper.MapAccount(rawAccount, platform);

			// Assert
			result.Should().BeEquivalentTo(rawAccount, options => options.ExcludingMissingMembers());
		}

		[Fact]
		public void ParseSymbolProfile_ShouldReturnCorrectSymbolProfile()
		{
			// Arrange
			var rawSymbolProfile = new Fixture().Create<Contract.SymbolProfile>();

			// Act
			var result = ContractToModelMapper.ParseSymbolProfile(rawSymbolProfile);

			// Assert
			result.Should().BeEquivalentTo(rawSymbolProfile, options => options.ExcludingMissingMembers());
		}

		[Fact]
		public void MapMarketDataList_ShouldReturnCorrectMarketDataProfile()
		{
			// Arrange
			var rawMarketDataList = new Fixture().Create<Contract.MarketDataList>();

			// Act
			var result = ContractToModelMapper.MapMarketDataList(rawMarketDataList);

			// Assert
			result.Should().BeEquivalentTo(rawMarketDataList, options => options.ExcludingMissingMembers());
		}

		[Fact]
		public void MapSymbolProfile_ShouldReturnCorrectSymbolProfile()
		{
			// Arrange
			var rawSymbolProfile = new Fixture().Create<Contract.SymbolProfile>();

			// Act
			var result = ContractToModelMapper.MapSymbolProfile(rawSymbolProfile);

			// Assert
			result.Should().BeEquivalentTo(rawSymbolProfile, options => options.ExcludingMissingMembers());
		}

		[Fact]
		public void MapMarketData_ShouldReturnCorrectMarketData()
		{
			// Arrange
			var rawMarketData = new Fixture().Create<Contract.MarketData>();

			// Act
			var result = ContractToModelMapper.MapMarketData(rawMarketData);

			// Assert
			result.Should().BeEquivalentTo(rawMarketData, options => options.ExcludingMissingMembers());
		}

		[Fact]
		public void MapToHoldings_ShouldReturnCorrectHoldings()
		{
			// Arrange
			var accounts = new Fixture().CreateMany<Model.Accounts.Account>(1).ToArray();
			var activities = new Fixture().CreateMany<Contract.Activity>(1).ToArray();

			// Act
			var result = ContractToModelMapper.MapToHoldings(accounts, activities);

			// Assert
			result.Should().BeEquivalentTo(activities, options => options.ExcludingMissingMembers());
		}
	}
}
