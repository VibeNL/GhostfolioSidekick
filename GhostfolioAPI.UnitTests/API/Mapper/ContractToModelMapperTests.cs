using AwesomeAssertions;
using GhostfolioSidekick.GhostfolioAPI.API.Mapper;
using GhostfolioSidekick.GhostfolioAPI.Contract;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;

namespace GhostfolioSidekick.GhostfolioAPI.UnitTests.API.Mapper
{
	public class ContractToModelMapperTests
	{
		[Fact]
		public void MapPlatform_ShouldMapCorrectly()
		{
			// Arrange
			Platform rawPlatform = new()
			{
				Name = "Test Platform",
				Url = "http://testplatform.com",
				Id = Guid.NewGuid().ToString()
			};

			// Act
			var result = ContractToModelMapper.MapPlatform(rawPlatform);

			// Assert
			_ = result.Name.Should().Be("Test Platform");
			_ = result.Url.Should().Be("http://testplatform.com");
		}

		[Fact]
		public void MapAccount_ShouldMapCorrectly()
		{
			// Arrange
			Account rawAccount = new()
			{
				Name = "Test Account",
				Comment = "Test Comment",
				Currency = "USD",
				Balance = 1000m,
				Id = Guid.NewGuid().ToString()
			};
			Platform rawPlatform = new()
			{
				Name = "Test Platform",
				Url = "http://testplatform.com",
				Id = Guid.NewGuid().ToString()
			};

			// Act
			var result = ContractToModelMapper.MapAccount(rawAccount, rawPlatform);

			// Assert
			_ = result.Name.Should().Be("Test Account");
			_ = result.Comment.Should().Be("Test Comment");
			_ = result.Platform.Should().NotBeNull();
			_ = result.Platform!.Name.Should().Be("Test Platform");
			_ = result.Balance.Should().HaveCount(1);
			_ = result.Balance.First().Money.Amount.Should().Be(1000m);
		}

		[Fact]
		public void MapSymbolProfile_ShouldMapCorrectly()
		{
			// Arrange
			SymbolProfile rawSymbolProfile = new()
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
			_ = result.Symbol.Should().Be("AAPL");
			_ = result.Name.Should().Be("Apple Inc.");
			_ = result.Currency.Symbol.Should().Be("USD");
			_ = result.DataSource.Should().Be("GHOSTFOLIO_Yahoo");
			_ = result.AssetClass.Should().Be(AssetClass.Equity);
			_ = result.AssetSubClass.Should().Be(AssetSubClass.Stock);
			_ = result.ISIN.Should().Be("US0378331005");
			_ = result.Comment.Should().Be("Test Comment");
			_ = result.CountryWeight.Should().HaveCount(1);
			_ = result.SectorWeights.Should().HaveCount(1);
		}

		#region MapActivity Tests

		[Fact]
		public void MapActivity_WithBuyActivity_ShouldCreateBuyActivity()
		{
			// Arrange
			var account = CreateTestAccount();
			var symbolProfile = CreateTestSymbolProfile();
			var contractActivity = CreateTestContractActivity(ActivityType.BUY, symbolProfile);
			List<SymbolProfile> symbols = [symbolProfile];

			// Act
			var result = ContractToModelMapper.MapActivity(account, symbols, contractActivity);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().BeOfType<BuyActivity>();

			BuyActivity buyActivity = (BuyActivity)result;
			_ = buyActivity.Account.Should().Be(account);
			_ = buyActivity.Date.Should().Be(contractActivity.Date);
			_ = buyActivity.Quantity.Should().Be(contractActivity.Quantity);
			_ = buyActivity.UnitPrice.Amount.Should().Be(contractActivity.UnitPrice);
			_ = buyActivity.UnitPrice.Currency.Symbol.Should().Be(symbolProfile.Currency);
			_ = buyActivity.TransactionId.Should().Be(contractActivity.ReferenceCode);
			_ = buyActivity.Description.Should().Be(contractActivity.Comment);
			_ = buyActivity.PartialSymbolIdentifiers.Should().HaveCount(1);
			_ = buyActivity.PartialSymbolIdentifiers.First().Identifier.Should().Be(symbolProfile.Symbol);
		}

		[Fact]
		public void MapActivity_WithBuyActivityAndFee_ShouldCreateBuyActivityWithFees()
		{
			// Arrange
			var account = CreateTestAccount();
			var symbolProfile = CreateTestSymbolProfile();
			var contractActivity = CreateTestContractActivity(ActivityType.BUY, symbolProfile, fee: 10.50m, feeCurrency: "USD");
			List<SymbolProfile> symbols = [symbolProfile];

			// Act
			var result = ContractToModelMapper.MapActivity(account, symbols, contractActivity);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().BeOfType<BuyActivity>();

			BuyActivity buyActivity = (BuyActivity)result;
			_ = buyActivity.Fees.Should().HaveCount(1);
			_ = buyActivity.Fees.First().Amount.Should().Be(10.50m);
			_ = buyActivity.Fees.First().Currency.Symbol.Should().Be("USD");
		}

		[Fact]
		public void MapActivity_WithSellActivity_ShouldCreateSellActivity()
		{
			// Arrange
			var account = CreateTestAccount();
			var symbolProfile = CreateTestSymbolProfile();
			var contractActivity = CreateTestContractActivity(ActivityType.SELL, symbolProfile, quantity: 50, unitPrice: 110.25m);
			List<SymbolProfile> symbols = [symbolProfile];

			// Act
			var result = ContractToModelMapper.MapActivity(account, symbols, contractActivity);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().BeOfType<SellActivity>();

			SellActivity sellActivity = (SellActivity)result;
			_ = sellActivity.Account.Should().Be(account);
			_ = sellActivity.Date.Should().Be(contractActivity.Date);
			_ = sellActivity.Quantity.Should().Be(50);
			_ = sellActivity.UnitPrice.Amount.Should().Be(110.25m);
		}

		[Fact]
		public void MapActivity_WithSellActivityAndFee_ShouldCreateSellActivityWithFees()
		{
			// Arrange
			var account = CreateTestAccount();
			var symbolProfile = CreateTestSymbolProfile();
			var contractActivity = CreateTestContractActivity(ActivityType.SELL, symbolProfile, fee: 5.25m);
			List<SymbolProfile> symbols = [symbolProfile];

			// Act
			var result = ContractToModelMapper.MapActivity(account, symbols, contractActivity);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().BeOfType<SellActivity>();

			SellActivity sellActivity = (SellActivity)result;
			_ = sellActivity.Fees.Should().HaveCount(1);
			_ = sellActivity.Fees.First().Amount.Should().Be(5.25m);
		}

		[Fact]
		public void MapActivity_WithDividendActivity_ShouldCreateDividendActivity()
		{
			// Arrange
			var account = CreateTestAccount();
			var symbolProfile = CreateTestSymbolProfile();
			var contractActivity = CreateTestContractActivity(ActivityType.DIVIDEND, symbolProfile, unitPrice: 2.50m);
			List<SymbolProfile> symbols = [symbolProfile];

			// Act
			var result = ContractToModelMapper.MapActivity(account, symbols, contractActivity);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().BeOfType<DividendActivity>();

			DividendActivity dividendActivity = (DividendActivity)result;
			_ = dividendActivity.Account.Should().Be(account);
			_ = dividendActivity.Date.Should().Be(contractActivity.Date);
			_ = dividendActivity.Amount.Amount.Should().Be(2.50m);
			_ = dividendActivity.Amount.Currency.Symbol.Should().Be(symbolProfile.Currency);
			_ = dividendActivity.PartialSymbolIdentifiers.Should().HaveCount(1);
		}

		[Fact]
		public void MapActivity_WithDividendActivityAndFee_ShouldCreateDividendActivityWithFees()
		{
			// Arrange
			var account = CreateTestAccount();
			var symbolProfile = CreateTestSymbolProfile();
			var contractActivity = CreateTestContractActivity(ActivityType.DIVIDEND, symbolProfile, fee: 0.25m);
			List<SymbolProfile> symbols = [symbolProfile];

			// Act
			var result = ContractToModelMapper.MapActivity(account, symbols, contractActivity);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().BeOfType<DividendActivity>();

			DividendActivity dividendActivity = (DividendActivity)result;
			_ = dividendActivity.Fees.Should().HaveCount(1);
			_ = dividendActivity.Fees.First().Amount.Should().Be(0.25m);
		}

		[Fact]
		public void MapActivity_WithInterestActivity_ShouldCreateInterestActivity()
		{
			// Arrange
			var account = CreateTestAccount();
			var symbolProfile = CreateTestSymbolProfile();
			var contractActivity = CreateTestContractActivity(ActivityType.INTEREST, symbolProfile, unitPrice: 15.75m);
			List<SymbolProfile> symbols = [symbolProfile];

			// Act
			var result = ContractToModelMapper.MapActivity(account, symbols, contractActivity);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().BeOfType<InterestActivity>();

			InterestActivity interestActivity = (InterestActivity)result;
			_ = interestActivity.Account.Should().Be(account);
			_ = interestActivity.Date.Should().Be(contractActivity.Date);
			_ = interestActivity.Amount.Amount.Should().Be(15.75m);
		}

		[Fact]
		public void MapActivity_WithFeeActivity_ShouldCreateFeeActivity()
		{
			// Arrange
			var account = CreateTestAccount();
			var symbolProfile = CreateTestSymbolProfile();
			var contractActivity = CreateTestContractActivity(ActivityType.FEE, symbolProfile, unitPrice: 5.00m);
			List<SymbolProfile> symbols = [symbolProfile];

			// Act
			var result = ContractToModelMapper.MapActivity(account, symbols, contractActivity);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().BeOfType<FeeActivity>();

			FeeActivity feeActivity = (FeeActivity)result;
			_ = feeActivity.Account.Should().Be(account);
			_ = feeActivity.Date.Should().Be(contractActivity.Date);
			_ = feeActivity.Amount.Amount.Should().Be(5.00m);
		}

		[Fact]
		public void MapActivity_WithValuableActivity_ShouldCreateValuableActivity()
		{
			// Arrange
			var account = CreateTestAccount();
			var symbolProfile = CreateTestSymbolProfile();
			var contractActivity = CreateTestContractActivity(ActivityType.ITEM, symbolProfile, unitPrice: 1000.00m);
			List<SymbolProfile> symbols = [symbolProfile];

			// Act
			var result = ContractToModelMapper.MapActivity(account, symbols, contractActivity);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().BeOfType<ValuableActivity>();

			ValuableActivity valuableActivity = (ValuableActivity)result;
			_ = valuableActivity.Account.Should().Be(account);
			_ = valuableActivity.Date.Should().Be(contractActivity.Date);
			_ = valuableActivity.Amount.Amount.Should().Be(1000.00m);
			_ = valuableActivity.PartialSymbolIdentifiers.Should().HaveCount(1);
		}

		[Fact]
		public void MapActivity_WithLiabilityActivity_ShouldCreateLiabilityActivity()
		{
			// Arrange
			var account = CreateTestAccount();
			var symbolProfile = CreateTestSymbolProfile();
			var contractActivity = CreateTestContractActivity(ActivityType.LIABILITY, symbolProfile, unitPrice: 500.00m);
			List<SymbolProfile> symbols = [symbolProfile];

			// Act
			var result = ContractToModelMapper.MapActivity(account, symbols, contractActivity);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().BeOfType<LiabilityActivity>();

			LiabilityActivity liabilityActivity = (LiabilityActivity)result;
			_ = liabilityActivity.Account.Should().Be(account);
			_ = liabilityActivity.Date.Should().Be(contractActivity.Date);
			_ = liabilityActivity.Amount.Amount.Should().Be(500.00m);
			_ = liabilityActivity.PartialSymbolIdentifiers.Should().HaveCount(1);
		}

		[Fact]
		public void MapActivity_WithUnsupportedActivityType_ShouldThrowNotSupportedException()
		{
			// Arrange
			var account = CreateTestAccount();
			var symbolProfile = CreateTestSymbolProfile();
			var contractActivity = CreateTestContractActivity((ActivityType)999, symbolProfile); // Invalid activity type
			List<SymbolProfile> symbols = [symbolProfile];

			// Act & Assert
			var exception = Assert.Throws<NotSupportedException>(() =>
				ContractToModelMapper.MapActivity(account, symbols, contractActivity));

			_ = exception.Message.Should().Contain("Activity type");
			_ = exception.Message.Should().Contain("is not supported");
		}

		[Fact]
		public void MapActivity_WithMissingSymbol_ShouldThrowArgumentException()
		{
			// Arrange
			var account = CreateTestAccount();
			var symbolProfile = CreateTestSymbolProfile();
			var contractActivity = CreateTestContractActivity(ActivityType.BUY, symbolProfile);
			List<SymbolProfile> symbols = []; // Empty symbol list

			// Act & Assert
			var exception = Assert.Throws<ArgumentException>(() =>
				ContractToModelMapper.MapActivity(account, symbols, contractActivity));

			_ = exception.Message.Should().Contain("Symbol");
			_ = exception.Message.Should().Contain("not found");
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
			List<SymbolProfile> symbols = [symbolProfile];

			// Act
			var result = ContractToModelMapper.MapActivity(account, symbols, contractActivity);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.TransactionId.Should().Be("TEST-ID-123");
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
			List<SymbolProfile> symbols = [symbolProfile];

			// Act
			var result = ContractToModelMapper.MapActivity(account, symbols, contractActivity);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.TransactionId.Should().NotBeNullOrEmpty();
			// Should be a valid GUID format
			_ = Guid.TryParse(result.TransactionId, out _).Should().BeTrue();
		}

		[Fact]
		public void MapActivity_WithDifferentCurrencies_ShouldHandleFeeCurrencyCorrectly()
		{
			// Arrange
			var account = CreateTestAccount();
			var symbolProfile = CreateTestSymbolProfile("AAPL", "USD");
			var contractActivity = CreateTestContractActivity(ActivityType.BUY, symbolProfile, fee: 15.00m, feeCurrency: "EUR");
			List<SymbolProfile> symbols = [symbolProfile];

			// Act
			var result = ContractToModelMapper.MapActivity(account, symbols, contractActivity);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().BeOfType<BuyActivity>();

			BuyActivity buyActivity = (BuyActivity)result;
			_ = buyActivity.Fees.Should().HaveCount(1);
			_ = buyActivity.Fees.First().Currency.Symbol.Should().Be("EUR");
		}

		[Fact]
		public void MapActivity_WithZeroFee_ShouldNotAddFeeToActivity()
		{
			// Arrange
			var account = CreateTestAccount();
			var symbolProfile = CreateTestSymbolProfile();
			var contractActivity = CreateTestContractActivity(ActivityType.BUY, symbolProfile, fee: 0.00m);
			List<SymbolProfile> symbols = [symbolProfile];

			// Act
			var result = ContractToModelMapper.MapActivity(account, symbols, contractActivity);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().BeOfType<BuyActivity>();

			BuyActivity buyActivity = (BuyActivity)result;
			_ = buyActivity.Fees.Should().BeEmpty();
		}

		[Fact]
		public void MapActivity_WithNullFeeCurrency_ShouldUseSymbolCurrency()
		{
			// Arrange
			var account = CreateTestAccount();
			var symbolProfile = CreateTestSymbolProfile("AAPL", "USD");
			var contractActivity = CreateTestContractActivity(ActivityType.SELL, symbolProfile, fee: 5.00m, feeCurrency: null);
			List<SymbolProfile> symbols = [symbolProfile];

			// Act
			var result = ContractToModelMapper.MapActivity(account, symbols, contractActivity);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().BeOfType<SellActivity>();

			SellActivity sellActivity = (SellActivity)result;
			_ = sellActivity.Fees.Should().HaveCount(1);
			_ = sellActivity.Fees.First().Currency.Symbol.Should().Be("USD");
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
			List<SymbolProfile> symbols = [symbolProfile];

			// Act
			var result = ContractToModelMapper.MapActivity(account, symbols, contractActivity);

			// Assert
			_ = result.Should().NotBeNull();
			_ = result.Should().BeOfType(expectedType);
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
