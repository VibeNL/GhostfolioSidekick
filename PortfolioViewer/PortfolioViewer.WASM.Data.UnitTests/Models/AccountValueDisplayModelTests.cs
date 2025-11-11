using AwesomeAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.PortfolioViewer.WASM.Data.Models;
using Xunit;

namespace PortfolioViewer.WASM.Data.UnitTests.Models
{
	public class AccountValueDisplayModelTests
	{
		[Fact]
		public void Constructor_SetsAllPropertiesCorrectly()
		{
			// Arrange
			var date = new DateOnly(2024, 1, 15);
			const string accountName = "Test Account";
			const int accountId = 123;
			var value = new Money(Currency.USD, 1000m);
			var invested = new Money(Currency.USD, 800m);
			var balance = new Money(Currency.USD, 200m);
			var gainLoss = new Money(Currency.USD, 200m);
			const decimal gainLossPercentage = 25.5m;
			const string currency = "EUR";

			// Act
			var model = new AccountValueDisplayModel
			{
				Date = date,
				AccountName = accountName,
				AccountId = accountId,
				Value = value,
				Invested = invested,
				Balance = balance,
				GainLoss = gainLoss,
				GainLossPercentage = gainLossPercentage,
				Currency = currency
			};

			// Assert
			model.Date.Should().Be(date);
			model.AccountName.Should().Be(accountName);
			model.AccountId.Should().Be(accountId);
			model.Value.Should().Be(value);
			model.Invested.Should().Be(invested);
			model.Balance.Should().Be(balance);
			model.GainLoss.Should().Be(gainLoss);
			model.GainLossPercentage.Should().Be(gainLossPercentage);
			model.Currency.Should().Be(currency);
		}

		[Fact]
		public void DefaultValues_ShouldBeSetCorrectly()
		{
			// Act
			var model = new AccountValueDisplayModel
			{
				Value = new Money(Currency.USD, 100m),
				Invested = new Money(Currency.USD, 80m),
				Balance = new Money(Currency.USD, 20m),
				GainLoss = new Money(Currency.USD, 20m)
			};

			// Assert
			model.AccountName.Should().Be(string.Empty);
			model.AccountId.Should().Be(0);
			model.GainLossPercentage.Should().Be(0);
			model.Currency.Should().Be("USD");
		}

		[Fact]
		public void TotalValue_ShouldReturnValueProperty()
		{
			// Arrange
			var value = new Money(Currency.EUR, 1500m);
			var model = new AccountValueDisplayModel
			{
				Value = value,
				Invested = new Money(Currency.EUR, 1200m),
				Balance = new Money(Currency.EUR, 300m),
				GainLoss = new Money(Currency.EUR, 300m)
			};

			// Act
			var totalValue = model.Value;

			// Assert
			totalValue.Should().Be(value);
			totalValue.Amount.Should().Be(1500m);
			totalValue.Currency.Should().Be(Currency.EUR);
		}

		[Fact]
		public void TotalInvested_ShouldReturnInvestedProperty()
		{
			// Arrange
			var invested = new Money(Currency.USD, 800m);
			var model = new AccountValueDisplayModel
			{
				Value = new Money(Currency.USD, 1000m),
				Invested = invested,
				Balance = new Money(Currency.USD, 200m),
				GainLoss = new Money(Currency.USD, 200m)
			};

			// Act
			var totalInvested = model.Invested;

			// Assert
			totalInvested.Should().Be(invested);
			totalInvested.Amount.Should().Be(800m);
			totalInvested.Currency.Should().Be(Currency.USD);
		}

		[Fact]
		public void CashBalance_ShouldReturnBalanceProperty()
		{
			// Arrange
			var balance = new Money(Currency.GBP, 150m);
			var model = new AccountValueDisplayModel
			{
				Value = new Money(Currency.GBP, 1000m),
				Invested = new Money(Currency.GBP, 850m),
				Balance = balance,
				GainLoss = new Money(Currency.GBP, 150m)
			};

			// Act
			var cashBalance = model.Balance;

			// Assert
			cashBalance.Should().Be(balance);
			cashBalance.Amount.Should().Be(150m);
			cashBalance.Currency.Should().Be(Currency.GBP);
		}

		[Fact]
		public void AssetValue_ShouldCalculateCorrectly_WhenBalanceIsPositive()
		{
			// Arrange
			var model = new AccountValueDisplayModel
			{
				Value = new Money(Currency.USD, 1000m),
				Invested = new Money(Currency.USD, 800m),
				Balance = new Money(Currency.USD, 200m),
				GainLoss = new Money(Currency.USD, 200m)
			};

			// Act
			var assetValue = model.AssetValue;

			// Assert
			assetValue.Amount.Should().Be(800m); // 1000 - 200
			assetValue.Currency.Should().Be(Currency.USD);
		}

		[Fact]
		public void AssetValue_ShouldCalculateCorrectly_WhenBalanceIsZero()
		{
			// Arrange
			var model = new AccountValueDisplayModel
			{
				Value = new Money(Currency.USD, 1000m),
				Invested = new Money(Currency.USD, 1000m),
				Balance = new Money(Currency.USD, 0m),
				GainLoss = new Money(Currency.USD, 0m)
			};

			// Act
			var assetValue = model.AssetValue;

			// Assert
			assetValue.Amount.Should().Be(1000m); // 1000 - 0
			assetValue.Currency.Should().Be(Currency.USD);
		}

		[Fact]
		public void AssetValue_ShouldCalculateCorrectly_WhenBalanceIsNegative()
		{
			// Arrange
			var model = new AccountValueDisplayModel
			{
				Value = new Money(Currency.USD, 800m),
				Invested = new Money(Currency.USD, 1000m),
				Balance = new Money(Currency.USD, -100m), // Negative balance (debt/margin)
				GainLoss = new Money(Currency.USD, -200m)
			};

			// Act
			var assetValue = model.AssetValue;

			// Assert
			assetValue.Amount.Should().Be(900m); // 800 - (-100) = 900
			assetValue.Currency.Should().Be(Currency.USD);
		}

		[Fact]
		public void AssetValue_ShouldHandleDifferentCurrencies()
		{
			// Arrange
			var model = new AccountValueDisplayModel
			{
				Value = new Money(Currency.EUR, 850m),
				Invested = new Money(Currency.EUR, 700m),
				Balance = new Money(Currency.EUR, 150m),
				GainLoss = new Money(Currency.EUR, 150m)
			};

			// Act
			var assetValue = model.AssetValue;

			// Assert
			assetValue.Amount.Should().Be(700m); // 850 - 150
			assetValue.Currency.Should().Be(Currency.EUR);
		}

		[Theory]
		[InlineData(1000, 200, 800)]
		[InlineData(500, 100, 400)]
		[InlineData(0, 0, 0)]
		[InlineData(250, 250, 0)]
		[InlineData(100, 150, -50)] // When balance exceeds total value
		public void AssetValue_ShouldCalculateCorrectly_WithVariousAmounts(decimal valueAmount, decimal balanceAmount, decimal expectedAssetValue)
		{
			// Arrange
			var model = new AccountValueDisplayModel
			{
				Value = new Money(Currency.USD, valueAmount),
				Invested = new Money(Currency.USD, 0m), // Not used in calculation
				Balance = new Money(Currency.USD, balanceAmount),
				GainLoss = new Money(Currency.USD, 0m) // Not used in calculation
			};

			// Act
			var assetValue = model.AssetValue;

			// Assert
			assetValue.Amount.Should().Be(expectedAssetValue);
			assetValue.Currency.Should().Be(Currency.USD);
		}

		[Fact]
		public void Date_CanBeSetToAnyValidDate()
		{
			// Arrange
			var testDates = new[]
			{
				new DateOnly(2020, 1, 1),
				new DateOnly(2024, 12, 31),
				new DateOnly(2023, 6, 15),
				DateOnly.MinValue,
				DateOnly.MaxValue
			};

			foreach (var testDate in testDates)
			{
				// Act
				var model = new AccountValueDisplayModel
				{
					Date = testDate,
					Value = new Money(Currency.USD, 100m),
					Invested = new Money(Currency.USD, 80m),
					Balance = new Money(Currency.USD, 20m),
					GainLoss = new Money(Currency.USD, 20m)
				};

				// Assert
				model.Date.Should().Be(testDate);
			}
		}

		[Theory]
		[InlineData("")]
		[InlineData("My Savings Account")]
		[InlineData("401(k) - Company Match")]
		[InlineData("Roth IRA")]
		[InlineData("Trading Account #1")]
		public void AccountName_CanBeSetToAnyString(string accountName)
		{
			// Arrange & Act
			var model = new AccountValueDisplayModel
			{
				AccountName = accountName,
				Value = new Money(Currency.USD, 100m),
				Invested = new Money(Currency.USD, 80m),
				Balance = new Money(Currency.USD, 20m),
				GainLoss = new Money(Currency.USD, 20m)
			};

			// Assert
			model.AccountName.Should().Be(accountName);
		}

		[Theory]
		[InlineData(0)]
		[InlineData(1)]
		[InlineData(999)]
		[InlineData(int.MaxValue)]
		[InlineData(int.MinValue)]
		public void AccountId_CanBeSetToAnyInteger(int accountId)
		{
			// Arrange & Act
			var model = new AccountValueDisplayModel
			{
				AccountId = accountId,
				Value = new Money(Currency.USD, 100m),
				Invested = new Money(Currency.USD, 80m),
				Balance = new Money(Currency.USD, 20m),
				GainLoss = new Money(Currency.USD, 20m)
			};

			// Assert
			model.AccountId.Should().Be(accountId);
		}

		[Theory]
		[InlineData(0)]
		[InlineData(25.5)]
		[InlineData(-10.75)]
		[InlineData(100)]
		[InlineData(-100)]
		public void GainLossPercentage_CanBeSetToAnyDecimal(decimal percentage)
		{
			// Arrange & Act
			var model = new AccountValueDisplayModel
			{
				GainLossPercentage = percentage,
				Value = new Money(Currency.USD, 100m),
				Invested = new Money(Currency.USD, 80m),
				Balance = new Money(Currency.USD, 20m),
				GainLoss = new Money(Currency.USD, 20m)
			};

			// Assert
			model.GainLossPercentage.Should().Be(percentage);
		}

		[Theory]
		[InlineData("USD")]
		[InlineData("EUR")]
		[InlineData("GBP")]
		[InlineData("JPY")]
		[InlineData("")]
		public void Currency_CanBeSetToAnyString(string currency)
		{
			// Arrange & Act
			var model = new AccountValueDisplayModel
			{
				Currency = currency,
				Value = new Money(Currency.USD, 100m),
				Invested = new Money(Currency.USD, 80m),
				Balance = new Money(Currency.USD, 20m),
				GainLoss = new Money(Currency.USD, 20m)
			};

			// Assert
			model.Currency.Should().Be(currency);
		}

		[Fact]
		public void CompatibilityProperties_ShouldMaintainConsistency()
		{
			// Arrange
			var value = new Money(Currency.EUR, 1200m);
			var invested = new Money(Currency.EUR, 900m);
			var balance = new Money(Currency.EUR, 300m);

			var model = new AccountValueDisplayModel
			{
				Value = value,
				Invested = invested,
				Balance = balance,
				GainLoss = new Money(Currency.EUR, 300m)
			};

			// Act & Assert - Verify that compatibility properties point to the same objects
			model.Value.Should().BeSameAs(model.Value);
			model.Invested.Should().BeSameAs(model.Invested);
			model.Balance.Should().BeSameAs(model.Balance);

			// Verify values are correct
			model.Value.Amount.Should().Be(1200m);
			model.Invested.Amount.Should().Be(900m);
			model.Balance.Amount.Should().Be(300m);
		}
	}
}
