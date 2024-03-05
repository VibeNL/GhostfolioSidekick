using Moq;
using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.GhostfolioAPI.API.Mapper;
using GhostfolioSidekick.Model.Compare;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.GhostfolioAPI.UnitTests.API.Mapper
{
	public class ModelToContractMapperTests
	{
		private readonly Mock<IExchangeRateService> exchangeRateServiceMock;

		public ModelToContractMapperTests()
		{
			exchangeRateServiceMock = new Mock<IExchangeRateService>();
			exchangeRateServiceMock.Setup(x => x.GetConversionRate(It.IsAny<Currency?>(), It.IsAny<Currency?>(), It.IsAny<DateTime>())).ReturnsAsync(1);
		}

		[Fact]
		public async Task ConvertToGhostfolioActivity_BuySellActivity_Success()
		{
			// Arrange
			var symbolProfile = DefaultFixture.Create().Create<Model.Symbols.SymbolProfile>();
			var buySellActivity = DefaultFixture.Create().Create<BuySellActivity>();
			
			// Act
			var result = await ModelToContractMapper.ConvertToGhostfolioActivity(exchangeRateServiceMock.Object, symbolProfile, buySellActivity);

			// Assert
			result.Should().NotBeNull();
			result.Type.Should().Be(Contract.ActivityType.BUY);
			result.SymbolProfile!.Symbol.Should().Be(symbolProfile.Symbol);
			result.SymbolProfile.DataSource.Should().Be(symbolProfile.DataSource);
			result.Date.Should().Be(buySellActivity.Date);
			result.Quantity.Should().Be(buySellActivity.Quantity);
			result.UnitPrice.Should().Be(buySellActivity.UnitPrice!.Amount);
			// TODO check more
		}

		[Fact]
		public async Task ConvertToGhostfolioActivity_DividendActivity_Success()
		{
			// Arrange
			var symbolProfile = DefaultFixture.Create().Create<Model.Symbols.SymbolProfile>();
			var dividendActivity = DefaultFixture.Create().Create<DividendActivity>();

			// Act
			var result = await ModelToContractMapper.ConvertToGhostfolioActivity(exchangeRateServiceMock.Object, symbolProfile, dividendActivity);

			// Assert
			result.Should().NotBeNull();
			result.Type.Should().Be(Contract.ActivityType.DIVIDEND);
			result.SymbolProfile!.Symbol.Should().Be(symbolProfile.Symbol);
			result.SymbolProfile.DataSource.Should().Be(symbolProfile.DataSource);
			result.Date.Should().Be(dividendActivity.Date);
			result.Quantity.Should().Be(1);
			result.UnitPrice.Should().Be(dividendActivity.Amount!.Amount);
			// TODO check more
		}

		[Fact]
		public async Task ConvertToGhostfolioActivity_UnknownActivityType_ThrowsException()
		{
			// Arrange
			var symbolProfile = DefaultFixture.Create().Create<Model.Symbols.SymbolProfile>();
			var unknownActivity = new Mock<IActivity>().Object;

			// Act
			Func<Task> act = async () => await ModelToContractMapper.ConvertToGhostfolioActivity(exchangeRateServiceMock.Object, symbolProfile, unknownActivity);

			// Assert
			await act.Should().ThrowAsync<NotSupportedException>();
		}
	}
}
