using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.GhostfolioAPI.API.Mapper;
using GhostfolioSidekick.Model.Activities;

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
			result.Should().BeEquivalentTo(rawAccount, options => options
				.ExcludingMissingMembers()
				.Excluding(x => x.Balance)); // TODO
		}

		[Fact]
		public void ParseSymbolProfile_ShouldReturnCorrectSymbolProfile()
		{
			// Arrange
			var rawSymbolProfile = new Fixture().Customize(new AssetCustomization()).Create<Contract.SymbolProfile>();

			// Act
			var result = ContractToModelMapper.ParseSymbolProfile(rawSymbolProfile);

			// Assert
			result.Should().BeEquivalentTo(rawSymbolProfile, options => options
				.ExcludingMissingMembers()
				.Excluding(x => x.Currency) // TODO
				.Excluding(x => x.AssetClass) // TODO
				.Excluding(x => x.AssetSubClass) // TODO
				.Excluding(x => x.ActivitiesCount) // TODO
				.Excluding(x => x.ScraperConfiguration) // TODO
			);
		}

		[Fact]
		public void MapMarketDataList_ShouldReturnCorrectMarketDataProfile()
		{
			// Arrange
			var rawMarketDataList = new Fixture().Customize(new AssetCustomization()).Create<Contract.MarketDataList>();

			// Act
			var result = ContractToModelMapper.MapMarketDataList(rawMarketDataList);

			// Assert
			result.Should().BeEquivalentTo(rawMarketDataList, options => options
				.ExcludingMissingMembers()
				.Excluding(x => x.AssetProfile.Currency) // TODO
				.Excluding(x => x.AssetProfile.AssetClass) // TODO
				.Excluding(x => x.AssetProfile.AssetSubClass)); // TODO
		}

		[Fact]
		public void MapSymbolProfile_ShouldReturnCorrectSymbolProfile()
		{
			// Arrange
			var rawSymbolProfile = new Fixture().Customize(new AssetCustomization()).Create<Contract.SymbolProfile>();

			// Act
			var result = ContractToModelMapper.MapSymbolProfile(rawSymbolProfile);

			// Assert
			result.Should().BeEquivalentTo(rawSymbolProfile, options => options
				.ExcludingMissingMembers()
				.Excluding(x => x.Currency) // TODO
				.Excluding(x => x.AssetClass) // TODO
				.Excluding(x => x.AssetSubClass)); // TODO
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
			var activities = new Fixture().Customize(new AssetCustomization())
				.Build<Contract.Activity>()
				.With(x => x.AccountId, accounts[0].Id)
				.CreateMany(1)
				.ToArray();

			// Act
			var result = ContractToModelMapper.MapToHoldings(accounts, activities);

			// Assert
			result.Should().BeEquivalentTo(activities, options => options
				.ExcludingMissingMembers()
				.Excluding(x => x.SymbolProfile.Currency) // TODO
				.Excluding(x => x.SymbolProfile.AssetClass) // TODO
				.Excluding(x => x.SymbolProfile.AssetSubClass)); // TODO
		}
	}

	internal class AssetCustomization : ICustomization
	{
		public void Customize(IFixture fixture)
		{
			fixture.Customize<Contract.Activity>(composer =>
			composer
				.With(p => p.Type, Contract.ActivityType.BUY));
			fixture.Customize<Contract.SymbolProfile>(composer =>
			composer
				.With(p => p.AssetClass, AssetClass.Equity.ToString().ToUpperInvariant())
				.With(p => p.AssetSubClass, AssetSubClass.Etf.ToString().ToUpperInvariant()));
		}
	}
}
