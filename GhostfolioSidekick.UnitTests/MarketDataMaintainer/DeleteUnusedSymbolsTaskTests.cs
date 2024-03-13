using FluentAssertions;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.Extensions.Logging;
using Moq;

namespace GhostfolioSidekick.MarketDataMaintainer.UnitTests
{
	public class DeleteUnusedSymbolsTaskTests
	{
		private readonly Mock<ILogger<DeleteUnusedSymbolsTask>> loggerMock;
		private readonly Mock<IMarketDataService> marketDataServiceMock;
		private readonly Mock<IApplicationSettings> applicationSettingsMock;
		private readonly DeleteUnusedSymbolsTask deleteUnusedSymbolsTask;

		public DeleteUnusedSymbolsTaskTests()
		{
			loggerMock = new Mock<ILogger<DeleteUnusedSymbolsTask>>();
			marketDataServiceMock = new Mock<IMarketDataService>();
			applicationSettingsMock = new Mock<IApplicationSettings>();

			deleteUnusedSymbolsTask = new DeleteUnusedSymbolsTask(
				loggerMock.Object,
				marketDataServiceMock.Object,
				applicationSettingsMock.Object);
		}

		[Fact]
		public async Task DoWork_ShouldNotDeleteSymbols_WhenDeleteUnusedSymbolsIsFalse()
		{
			// Arrange
			applicationSettingsMock.Setup(x => x.ConfigurationInstance).Returns(new ConfigurationInstance() { Settings = new Settings() { DeleteUnusedSymbols = false } });

			// Act
			await deleteUnusedSymbolsTask.DoWork();

			// Assert
			marketDataServiceMock.Verify(x => x.DeleteSymbol(It.IsAny<SymbolProfile>()), Times.Never);
		}

		[Fact]
		public async Task DoWork_ShouldDeleteUnusedSymbols_WhenDeleteUnusedSymbolsIsTrue()
		{
			// Arrange
			applicationSettingsMock.Setup(x => x.ConfigurationInstance).Returns(new ConfigurationInstance() { Settings = new Settings() { DeleteUnusedSymbols = true } });

			var symbolProfiles = new List<SymbolProfile>
			{
				new SymbolProfile(Guid.NewGuid().ToString(), "A", Currency.USD, Datasource.MANUAL, Model.Activities.AssetClass.Equity, null, [], []),
				new SymbolProfile(Guid.NewGuid().ToString(), "B", Currency.USD, Datasource.MANUAL, Model.Activities.AssetClass.Equity, null, [], []){
					ActivitiesCount = 1
				},
				new SymbolProfile("SymbolA", "A", Currency.USD, Datasource.YAHOO, Model.Activities.AssetClass.Equity, null, [], [])
			};

			marketDataServiceMock.Setup(x => x.GetAllSymbolProfiles()).ReturnsAsync(symbolProfiles);
			marketDataServiceMock.Setup(x => x.GetInfo()).ReturnsAsync(new GhostfolioAPI.Contract.GenericInfo());

			// Act
			await deleteUnusedSymbolsTask.DoWork();

			// Assert
			marketDataServiceMock.Verify(x => x.DeleteSymbol(It.IsAny<SymbolProfile>()), Times.Exactly(2));
		}

		[Fact]
		public async Task DoWork_ShouldHandleNotAuthorizedException()
		{
			// Arrange
			applicationSettingsMock.Setup(x => x.ConfigurationInstance).Returns(new ConfigurationInstance() { Settings = new Settings() { DeleteUnusedSymbols = true } });
			marketDataServiceMock.Setup(x => x.GetAllSymbolProfiles()).ThrowsAsync(new NotAuthorizedException());

			// Act
			Func<Task> act = async () => await deleteUnusedSymbolsTask.DoWork();

			// Assert
			await act.Should().NotThrowAsync<NotAuthorizedException>();
			applicationSettingsMock.VerifySet(x => x.AllowAdminCalls = false);
		}

		[Fact]
		public async Task DoWork_ShouldNotDeleteUnusedBenchmarks_WhenDeleteUnusedSymbolsIsTrue()
		{
			// Arrange
			applicationSettingsMock.Setup(x => x.ConfigurationInstance).Returns(new ConfigurationInstance() { Settings = new Settings() { DeleteUnusedSymbols = true } });

			var symbolProfiles = new List<SymbolProfile>
			{
				new SymbolProfile("SymbolA", "A", Currency.USD, Datasource.YAHOO, Model.Activities.AssetClass.Equity, null, [], [])
			};

			marketDataServiceMock.Setup(x => x.GetAllSymbolProfiles()).ReturnsAsync(symbolProfiles);
			marketDataServiceMock.Setup(x => x.GetInfo()).ReturnsAsync(new GhostfolioAPI.Contract.GenericInfo()
			{
				BenchMarks = [new GhostfolioAPI.Contract.BenchMark {
					Name = "SymbolA",
					Symbol = "SymbolA"
			}]
			});

			// Act
			await deleteUnusedSymbolsTask.DoWork();

			// Assert
			marketDataServiceMock.Verify(x => x.DeleteSymbol(It.IsAny<SymbolProfile>()), Times.Never);
		}

		[Fact]
		public async Task DoWork_ShouldNotDeleteFearAndGreedIndex_WhenDeleteUnusedSymbolsIsTrue()
		{
			// Arrange
			applicationSettingsMock.Setup(x => x.ConfigurationInstance).Returns(new ConfigurationInstance() { Settings = new Settings() { DeleteUnusedSymbols = true } });

			var symbolProfiles = new List<SymbolProfile>
			{
				new SymbolProfile("_GF_FEAR_AND_GREED_INDEX", "A", Currency.USD, Datasource.YAHOO, Model.Activities.AssetClass.Equity, null, [], [])
			};

			marketDataServiceMock.Setup(x => x.GetAllSymbolProfiles()).ReturnsAsync(symbolProfiles);
			marketDataServiceMock.Setup(x => x.GetInfo()).ReturnsAsync(new GhostfolioAPI.Contract.GenericInfo()
			{
				BenchMarks = [new GhostfolioAPI.Contract.BenchMark {
					Name = "_GF_FEAR_AND_GREED_INDEX",
					Symbol = "_GF_FEAR_AND_GREED_INDEX"
			}]
			});

			// Act
			await deleteUnusedSymbolsTask.DoWork();

			// Assert
			marketDataServiceMock.Verify(x => x.DeleteSymbol(It.IsAny<SymbolProfile>()), Times.Never);
		}
	}
}
