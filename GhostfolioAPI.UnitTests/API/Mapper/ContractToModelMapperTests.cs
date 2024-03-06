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
				.Excluding(x => x.Balance));
			result.Balance.Money.Amount.Should().Be(rawAccount.Balance);
		}

		[Fact]
		public void ParseSymbolProfile_ShouldReturnCorrectSymbolProfile()
		{
			// Arrange
			var rawSymbolProfile = new Fixture().Customize(new AssetCustomization()).Create<Contract.SymbolProfile>();

			// Act
			var result = ContractToModelMapper.MapSymbolProfile(rawSymbolProfile);

			// Assert
			result.Should().BeEquivalentTo(rawSymbolProfile, options => options
				.ExcludingMissingMembers()
				.Excluding(x => x.Currency)
				.Excluding(x => x.AssetClass)
				.Excluding(x => x.AssetSubClass)
				.Excluding(x => x.ActivitiesCount)
				.Excluding(x => x.ScraperConfiguration)
			);
			result.Currency.Symbol.Should().Be(rawSymbolProfile.Currency);
			result.AssetClass.Should().Be(Utilities.ParseAssetClass(rawSymbolProfile.AssetClass));
			result.AssetSubClass.Should().Be(Utilities.ParseAssetSubClass(rawSymbolProfile.AssetSubClass));
			result.ActivitiesCount.Should().Be(rawSymbolProfile.ActivitiesCount);
			result.ScraperConfiguration.Should().BeEquivalentTo(rawSymbolProfile.ScraperConfiguration, options => options.ExcludingMissingMembers());
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
				.Excluding(x => x.AssetProfile.Currency)
				.Excluding(x => x.AssetProfile.AssetClass)
				.Excluding(x => x.AssetProfile.AssetSubClass)
				.Excluding(x => x.MarketData));

			result.AssetProfile.Currency.Symbol.Should().Be(rawMarketDataList.AssetProfile.Currency);
			result.AssetProfile.AssetClass.Should().Be(Utilities.ParseAssetClass(rawMarketDataList.AssetProfile.AssetClass));
			result.AssetProfile.AssetSubClass.Should().Be(Utilities.ParseAssetSubClass(rawMarketDataList.AssetProfile.AssetSubClass));

			for (int i = 0; i < result.MarketData.Count; i++)
			{
				result.MarketData[i].Date.Should().Be(rawMarketDataList.MarketData[i].Date);
				result.MarketData[i].MarketPrice.Amount.Should().Be(rawMarketDataList.MarketData[i].MarketPrice);
			}
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
				.Excluding(x => x.Currency)
				.Excluding(x => x.AssetClass)
				.Excluding(x => x.AssetSubClass));
			result.Currency.Symbol.Should().Be(rawSymbolProfile.Currency);
			result.AssetClass.Should().Be(Utilities.ParseAssetClass(rawSymbolProfile.AssetClass));
			result.AssetSubClass.Should().Be(Utilities.ParseAssetSubClass(rawSymbolProfile.AssetSubClass));
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
			var result = ContractToModelMapper.MapToHoldings(accounts, activities).ToList();

			// Assert
			result.Should().BeEquivalentTo(activities, options => options
				.ExcludingMissingMembers()
				.Excluding(x => x.SymbolProfile.Currency)
				.Excluding(x => x.SymbolProfile.AssetClass)
				.Excluding(x => x.SymbolProfile.AssetSubClass));
			result[0].SymbolProfile.Currency.Symbol.Should().Be(activities[0].SymbolProfile.Currency);
			result[0].SymbolProfile.AssetClass.Should().Be(Utilities.ParseAssetClass(activities[0].SymbolProfile.AssetClass));
			result[0].SymbolProfile.AssetSubClass.Should().Be(Utilities.ParseAssetSubClass(activities[0].SymbolProfile.AssetSubClass));
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
