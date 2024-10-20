//using AutoFixture;
//using FluentAssertions;
//using GhostfolioSidekick.Configuration;
//using GhostfolioSidekick.GhostfolioAPI.API;
//using GhostfolioSidekick.GhostfolioAPI.Strategies;
//using GhostfolioSidekick.Model;
//using GhostfolioSidekick.Model.Activities;
//using GhostfolioSidekick.Model.Activities.Types;
//using GhostfolioSidekick.Model.Compare;
//using GhostfolioSidekick.Model.Symbols;
//using Microsoft.Extensions.Logging;
//using Moq;
//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using Xunit;

//namespace GhostfolioSidekick.GhostfolioAPI.UnitTests.Strategies
//{
//    public class HandleTaxesOnDividendsTests
//    {
//        private readonly Mock<IExchangeRateService> exchangeRateServiceMock;
//        private readonly HandleTaxesOnDividends handleTaxesOnDividends;
//        private readonly Settings settings;

//        public HandleTaxesOnDividendsTests()
//        {
//            settings = new Settings { SubstractTaxesOnDividendFromDividend = true };
//            exchangeRateServiceMock = new Mock<IExchangeRateService>();
//			exchangeRateServiceMock.Setup(x => x.GetConversionRate(It.IsAny<Currency>(), It.IsAny<Currency>(), It.IsAny<DateTime>())).ReturnsAsync(1);

//			handleTaxesOnDividends = new HandleTaxesOnDividends(settings, exchangeRateServiceMock.Object);
//        }

//        [Fact]
//        public async Task Execute_SubstractTaxesOnDividendFromDividendIsFalse_ShouldNotSubstractTaxes()
//        {
//            // Arrange
//            settings.SubstractTaxesOnDividendFromDividend = false;
//			var symbolProfile = new Fixture().Create<SymbolProfile>();
//			var dividendActivity = new DividendActivity(null!, DateTime.Now, new Money(Currency.USD, 100), string.Empty)
//			{
//				Taxes = new List<Money> { new Money(Currency.USD, 10) }
//			};
//			var holding = new Holding(symbolProfile)
//			{
//				Activities =
//				[
//					dividendActivity
//				]
//			};

//            // Act
//            await handleTaxesOnDividends.Execute(holding);

//            // Assert
//            dividendActivity.Amount.Amount.Should().Be(100);
//			dividendActivity.Taxes.Should().HaveCount(1);
//        }

//        [Fact]
//        public async Task Execute_SubstractTaxesOnDividendFromDividendIsTrue_ShouldSubstractTaxes()
//        {
//			// Arrange
//			settings.SubstractTaxesOnDividendFromDividend = true;
//			var symbolProfile = new Fixture().Create<SymbolProfile>();
//			var dividendActivity = new DividendActivity(null!, DateTime.Now, new Money(Currency.USD, 100), string.Empty)
//			{
//				Taxes = new List<Money> { new Money(Currency.USD, 10) }
//			};
//			var holding = new Holding(symbolProfile)
//			{
//				Activities =
//				[
//					dividendActivity
//				]
//			};

//			// Act
//			await handleTaxesOnDividends.Execute(holding);

//			// Assert
//			dividendActivity.Amount.Amount.Should().Be(90);
//			dividendActivity.Taxes.Should().HaveCount(0);
//        }

//		[Fact]
//		public async Task Execute_TaxCurrencyDiffersFromDividendCurrency_ShouldConvertTaxCurrency()
//		{
//			settings.SubstractTaxesOnDividendFromDividend = true;
//			var symbolProfile = new Fixture().Create<SymbolProfile>();
//			var dividendActivity = new DividendActivity(null!, DateTime.Now, new Money(Currency.USD, 100), string.Empty)
//			{
//				Taxes = new List<Money> { new Money(Currency.EUR, 10) }
//			};
//			var holding = new Holding(symbolProfile)
//			{
//				Activities =
//				[
//					dividendActivity
//				]
//			};
//			exchangeRateServiceMock.Setup(x => x.GetConversionRate(Currency.EUR, Currency.USD, It.IsAny<DateTime>())).ReturnsAsync(1.2m);

//			// Act
//			await handleTaxesOnDividends.Execute(holding);

//			// Assert
//			dividendActivity.Amount.Amount.Should().BeApproximately(88, 0.01m);
//			dividendActivity.Taxes.Should().HaveCount(0);
//		}
//	}
//}
