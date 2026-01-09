using AwesomeAssertions;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.GhostfolioAPI.API.Mapper;
using GhostfolioSidekick.GhostfolioAPI.Contract;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using Moq;

namespace GhostfolioSidekick.GhostfolioAPI.UnitTests.API.Mapper
{
	public class ContractToModelMapperTests
	{
		[Fact]
		public void MapPlatform_ShouldMapCorrectly()
		{
			// Arrange
			var rawPlatform = new Platform
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
			var rawAccount = new Account
			{
				Name = "Test Account",
				Comment = "Test Comment",
				Currency = "USD",
				Balance = 1000m,
				Id = Guid.NewGuid().ToString()
			};
			var rawPlatform = new Platform
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
			var rawSymbolProfile = new SymbolProfile
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
					new Country { Name = "United States", Code = "US", Continent = "North America", Weight = 100m }
				],
				Sectors =
				[
					new Sector { Name = "Technology", Weight = 100m }
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

		#region MapActivity Tests

		[Fact]
		public void MapActivity_WithBuyActivity_ShouldCreateBuyActivity()
		{
			// Arrange
			var account = CreateTestAccount();
			var symbolProfile = CreateTestSymbolProfile();
			var contractActivity = CreateTestContractActivity(ActivityType.BUY, symbolProfile);
			var symbols = new List<SymbolProfile> { symbolProfile };

			// Act
			var result = ContractToModelMapper.MapActivity(account, symbols, contractActivity);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeOfType<BuyActivity>();

			var buyActivity = (BuyActivity)result;
			buyActivity.Account.Should().Be(account);
			buyActivity.Date.Should().Be(contractActivity.Date);
			buyActivity.Quantity.Should().Be(contractActivity.Quantity);
			buyActivity.UnitPrice.Amount.Should().Be(contractActivity.UnitPrice);
			buyActivity.UnitPrice.Currency.Symbol.Should().Be(symbolProfile.Currency);
			buyActivity.TransactionId.Should().Be(contractActivity.ReferenceCode);
			buyActivity.Description.Should().Be(contractActivity.Comment);
			buyActivity.TotalTransactionAmount.Amount.Should().Be(contractActivity.Quantity * contractActivity.UnitPrice);
			buyActivity.PartialSymbolIdentifiers.Should().HaveCount(1);
			buyActivity.PartialSymbolIdentifiers.First().Identifier.Should().Be(symbolProfile.Symbol);
		}

		[Fact]
		public void MapActivity_WithBuyActivityAndFee_ShouldCreateBuyActivityWithFees()
		{
			// Arrange
			var account = CreateTestAccount();
			var symbolProfile = CreateTestSymbolProfile();
			var contractActivity = CreateTestContractActivity(ActivityType.BUY, symbolProfile, fee: 10.50m, feeCurrency: "USD");
			var symbols = new List<SymbolProfile> { symbolProfile };

			// Act
			var result = ContractToModelMapper.MapActivity(account, symbols, contractActivity);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeOfType<BuyActivity>();

			var buyActivity = (BuyActivity)result;
			buyActivity.Fees.Should().HaveCount(1);
			buyActivity.Fees.First().Money.Amount.Should().Be(10.50m);
			buyActivity.Fees.First().Money.Currency.Symbol.Should().Be("USD");
		}

		[Fact]
		public void MapActivity_WithSellActivity_ShouldCreateSellActivity()
		{
			// Arrange
			var account = CreateTestAccount();
			var symbolProfile = CreateTestSymbolProfile();
			var contractActivity = CreateTestContractActivity(ActivityType.SELL, symbolProfile, quantity: 50, unitPrice: 110.25m);
			var symbols = new List<SymbolProfile> { symbolProfile };

			// Act
			var result = ContractToModelMapper.MapActivity(account, symbols, contractActivity);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeOfType<SellActivity>();

			var sellActivity = (SellActivity)result;
			sellActivity.Account.Should().Be(account);
			sellActivity.Date.Should().Be(contractActivity.Date);
			sellActivity.Quantity.Should().Be(50);
			sellActivity.UnitPrice.Amount.Should().Be(110.25m);
			sellActivity.TotalTransactionAmount.Amount.Should().Be(50 * 110.25m);
		}

		[Fact]
		public void MapActivity_WithSellActivityAndFee_ShouldCreateSellActivityWithFees()
		{
			// Arrange
			var account = CreateTestAccount();
			var symbolProfile = CreateTestSymbolProfile();
			var contractActivity = CreateTestContractActivity(ActivityType.SELL, symbolProfile, fee: 5.25m);
			var symbols = new List<SymbolProfile> { symbolProfile };

			// Act
			var result = ContractToModelMapper.MapActivity(account, symbols, contractActivity);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeOfType<SellActivity>();

			var sellActivity = (SellActivity)result;
			sellActivity.Fees.Should().HaveCount(1);
			sellActivity.Fees.First().Money.Amount.Should().Be(5.25m);
		}

		[Fact]
		public void MapActivity_WithDividendActivity_ShouldCreateDividendActivity()
		{
			// Arrange
			var account = CreateTestAccount();
			var symbolProfile = CreateTestSymbolProfile();
			var contractActivity = CreateTestContractActivity(ActivityType.DIVIDEND, symbolProfile, unitPrice: 2.50m);
			var symbols = new List<SymbolProfile> { symbolProfile };

			// Act
			var result = ContractToModelMapper.MapActivity(account, symbols, contractActivity);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeOfType<DividendActivity>();

			var dividendActivity = (DividendActivity)result;
			dividendActivity.Account.Should().Be(account);
			dividendActivity.Date.Should().Be(contractActivity.Date);
			dividendActivity.Amount.Amount.Should().Be(2.50m);
			dividendActivity.Amount.Currency.Symbol.Should().Be(symbolProfile.Currency);
			dividendActivity.PartialSymbolIdentifiers.Should().HaveCount(1);
		}

		[Fact]
		public void MapActivity_WithDividendActivityAndFee_ShouldCreateDividendActivityWithFees()
		{
			// Arrange
			var account = CreateTestAccount();
			var symbolProfile = CreateTestSymbolProfile();
			var contractActivity = CreateTestContractActivity(ActivityType.DIVIDEND, symbolProfile, fee: 0.25m);
			var symbols = new List<SymbolProfile> { symbolProfile };

			// Act
			var result = ContractToModelMapper.MapActivity(account, symbols, contractActivity);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeOfType<DividendActivity>();

			var dividendActivity = (DividendActivity)result;
			dividendActivity.Fees.Should().HaveCount(1);
			dividendActivity.Fees.First().Money.Amount.Should().Be(0.25m);
		}

		[Fact]
		public void MapActivity_WithInterestActivity_ShouldCreateInterestActivity()
		{
			// Arrange
			var account = CreateTestAccount();
			var symbolProfile = CreateTestSymbolProfile();
			var contractActivity = CreateTestContractActivity(ActivityType.INTEREST, symbolProfile, unitPrice: 15.75m);
			var symbols = new List<SymbolProfile> { symbolProfile };

			// Act
			var result = ContractToModelMapper.MapActivity(account, symbols, contractActivity);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeOfType<InterestActivity>();

			var interestActivity = (InterestActivity)result;
			interestActivity.Account.Should().Be(account);
			interestActivity.Date.Should().Be(contractActivity.Date);
			interestActivity.Amount.Amount.Should().Be(15.75m);
		}

		[Fact]
		public void MapActivity_WithFeeActivity_ShouldCreateFeeActivity()
		{
			// Arrange
			var account = CreateTestAccount();
			var symbolProfile = CreateTestSymbolProfile();
			var contractActivity = CreateTestContractActivity(ActivityType.FEE, symbolProfile, unitPrice: 5.00m);
			var symbols = new List<SymbolProfile> { symbolProfile };

			// Act
			var result = ContractToModelMapper.MapActivity(account, symbols, contractActivity);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeOfType<FeeActivity>();

			var feeActivity = (FeeActivity)result;
			feeActivity.Account.Should().Be(account);
			feeActivity.Date.Should().Be(contractActivity.Date);
			feeActivity.Amount.Amount.Should().Be(5.00m);
		}

		[Fact]
		public void MapActivity_WithValuableActivity_ShouldCreateValuableActivity()
		{
			// Arrange
			var account = CreateTestAccount();
			var symbolProfile = CreateTestSymbolProfile();
			var contractActivity = CreateTestContractActivity(ActivityType.ITEM, symbolProfile, unitPrice: 1000.00m);
			var symbols = new List<SymbolProfile> { symbolProfile };

			// Act
			var result = ContractToModelMapper.MapActivity(account, symbols, contractActivity);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeOfType<ValuableActivity>();

			var valuableActivity = (ValuableActivity)result;
			valuableActivity.Account.Should().Be(account);
			valuableActivity.Date.Should().Be(contractActivity.Date);
			valuableActivity.Amount.Amount.Should().Be(1000.00m);
			valuableActivity.PartialSymbolIdentifiers.Should().HaveCount(1);
		}

		[Fact]
		public void MapActivity_WithLiabilityActivity_ShouldCreateLiabilityActivity()
		{
			// Arrange
			var account = CreateTestAccount();
			var symbolProfile = CreateTestSymbolProfile();
			var contractActivity = CreateTestContractActivity(ActivityType.LIABILITY, symbolProfile, unitPrice: 500.00m);
			var symbols = new List<SymbolProfile> { symbolProfile };

			// Act
			var result = ContractToModelMapper.MapActivity(account, symbols, contractActivity);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeOfType<LiabilityActivity>();

			var liabilityActivity = (LiabilityActivity)result;
			liabilityActivity.Account.Should().Be(account);
			liabilityActivity.Date.Should().Be(contractActivity.Date);
			liabilityActivity.Amount.Amount.Should().Be(500.00m);
			liabilityActivity.PartialSymbolIdentifiers.Should().HaveCount(1);
		}

		[Fact]
		public void MapActivity_WithUnsupportedActivityType_ShouldThrowNotSupportedException()
		{
			// Arrange
			var account = CreateTestAccount();
			var symbolProfile = CreateTestSymbolProfile();
			var contractActivity = CreateTestContractActivity((ActivityType)999, symbolProfile); // Invalid activity type
			var symbols = new List<SymbolProfile> { symbolProfile };

			// Act & Assert
			var exception = Assert.Throws<NotSupportedException>(() =>
				ContractToModelMapper.MapActivity(account, symbols, contractActivity));

			exception.Message.Should().Contain("Activity type");
			exception.Message.Should().Contain("is not supported");
		}

		[Fact]
		public void MapActivity_WithMissingSymbol_ShouldThrowArgumentException()
		{
			// Arrange
			var account = CreateTestAccount();
			var symbolProfile = CreateTestSymbolProfile();
			var contractActivity = CreateTestContractActivity(ActivityType.BUY, symbolProfile);
			var symbols = new List<SymbolProfile>(); // Empty symbol list

			// Act & Assert
			var exception = Assert.Throws<ArgumentException>(() =>
				ContractToModelMapper.MapActivity(account, symbols, contractActivity));

			exception.Message.Should().Contain("Symbol");
			exception.Message.Should().Contain("not found");
		}

		[Fact]
		public void MapActivity_WithNullReferenceCode_ShouldUseIdAsTransactionId()
		{
			// Arrange
			var account = CreateTestAccount();
			var symbolProfile = CreateTestSymbolProfile();
			var contractActivity = CreateTestContractActivity(ActivityType.BUY, symbolProfile);
			contractActivity.ReferenceCode = null;
			contractActivity.Id = "TEST-ID-123";
			var symbols = new List<SymbolProfile> { symbolProfile };

			// Act
			var result = ContractToModelMapper.MapActivity(account, symbols, contractActivity);

			// Assert
			result.Should().NotBeNull();
			result.TransactionId.Should().Be("TEST-ID-123");
		}

		[Fact]
		public void MapActivity_WithNullReferenceCodeAndId_ShouldGenerateGuidAsTransactionId()
		{
			// Arrange
			var account = CreateTestAccount();
			var symbolProfile = CreateTestSymbolProfile();
			var contractActivity = CreateTestContractActivity(ActivityType.BUY, symbolProfile);
			contractActivity.ReferenceCode = null;
			contractActivity.Id = null;
			var symbols = new List<SymbolProfile> { symbolProfile };

			// Act
			var result = ContractToModelMapper.MapActivity(account, symbols, contractActivity);

			// Assert
			result.Should().NotBeNull();
			result.TransactionId.Should().NotBeNullOrEmpty();
			// Should be a valid GUID format
			Guid.TryParse(result.TransactionId, out _).Should().BeTrue();
		}

		[Fact]
		public void MapActivity_WithDifferentCurrencies_ShouldHandleFeeCurrencyCorrectly()
		{
			// Arrange
			var account = CreateTestAccount();
			var symbolProfile = CreateTestSymbolProfile("AAPL", "USD");
			var contractActivity = CreateTestContractActivity(ActivityType.BUY, symbolProfile, fee: 15.00m, feeCurrency: "EUR");
			var symbols = new List<SymbolProfile> { symbolProfile };

			// Act
			var result = ContractToModelMapper.MapActivity(account, symbols, contractActivity);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeOfType<BuyActivity>();

			var buyActivity = (BuyActivity)result;
			buyActivity.Fees.Should().HaveCount(1);
			buyActivity.Fees.First().Money.Currency.Symbol.Should().Be("EUR");
		}

		[Fact]
		public void MapActivity_WithZeroFee_ShouldNotAddFeeToActivity()
		{
			// Arrange
			var account = CreateTestAccount();
			var symbolProfile = CreateTestSymbolProfile();
			var contractActivity = CreateTestContractActivity(ActivityType.BUY, symbolProfile, fee: 0.00m);
			var symbols = new List<SymbolProfile> { symbolProfile };

			// Act
			var result = ContractToModelMapper.MapActivity(account, symbols, contractActivity);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeOfType<BuyActivity>();

			var buyActivity = (BuyActivity)result;
			buyActivity.Fees.Should().BeEmpty();
		}

		[Fact]
		public void MapActivity_WithNullFeeCurrency_ShouldUseSymbolCurrency()
		{
			// Arrange
			var account = CreateTestAccount();
			var symbolProfile = CreateTestSymbolProfile("AAPL", "USD");
			var contractActivity = CreateTestContractActivity(ActivityType.SELL, symbolProfile, fee: 5.00m, feeCurrency: null);
			var symbols = new List<SymbolProfile> { symbolProfile };

			// Act
			var result = ContractToModelMapper.MapActivity(account, symbols, contractActivity);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeOfType<SellActivity>();

			var sellActivity = (SellActivity)result;
			sellActivity.Fees.Should().HaveCount(1);
			sellActivity.Fees.First().Money.Currency.Symbol.Should().Be("USD");
		}

		[Theory]
		[InlineData(ActivityType.BUY, typeof(BuyActivity))]
		[InlineData(ActivityType.SELL, typeof(SellActivity))]
		[InlineData(ActivityType.DIVIDEND, typeof(DividendActivity))]
		[InlineData(ActivityType.INTEREST, typeof(InterestActivity))]
		[InlineData(ActivityType.FEE, typeof(FeeActivity))]
		[InlineData(ActivityType.ITEM, typeof(ValuableActivity))]
		[InlineData(ActivityType.LIABILITY, typeof(LiabilityActivity))]
		public void MapActivity_WithAllSupportedActivityTypes_ShouldCreateCorrectActivityType(ActivityType activityType, Type expectedType)
		{
			// Arrange
			var account = CreateTestAccount();
			var symbolProfile = CreateTestSymbolProfile();
			var contractActivity = CreateTestContractActivity(activityType, symbolProfile);
			var symbols = new List<SymbolProfile> { symbolProfile };

			// Act
			var result = ContractToModelMapper.MapActivity(account, symbols, contractActivity);

			// Assert
			result.Should().NotBeNull();
			result.Should().BeOfType(expectedType);
		}

		#endregion

		#region Helper Methods

		private static Model.Accounts.Account CreateTestAccount(string name = "Test Account")
		{
			return new Model.Accounts.Account(name)
			{
				Id = 1,
				Comment = "Test account for unit tests"
			};
		}

		private static SymbolProfile CreateTestSymbolProfile(string symbol = "AAPL", string currency = "USD")
		{
			return new SymbolProfile
			{
				Symbol = symbol,
				Currency = currency,
				Name = "Apple Inc.",
				DataSource = "YAHOO",
				AssetClass = "EQUITY",
				AssetSubClass = "STOCK",
				Countries = [],
				Sectors = []
			};
		}

		private static Contract.Activity CreateTestContractActivity(
			ActivityType activityType,
			SymbolProfile symbolProfile,
			decimal quantity = 10,
			decimal unitPrice = 100.00m,
			decimal fee = 0.00m,
			string? feeCurrency = null,
			string? comment = "Test activity",
			string? referenceCode = "REF-001")
		{
			return new Contract.Activity
			{
				Id = Guid.NewGuid().ToString(),
				AccountId = "1",
				SymbolProfile = symbolProfile,
				Type = activityType,
				Date = DateTime.UtcNow.AddDays(-1),
				Quantity = quantity,
				UnitPrice = unitPrice,
				Fee = fee,
				FeeCurrency = feeCurrency,
				Comment = comment,
				ReferenceCode = referenceCode
			};
		}

		#endregion
	}
}
