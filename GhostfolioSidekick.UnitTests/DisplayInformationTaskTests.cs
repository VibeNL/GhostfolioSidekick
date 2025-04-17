using GhostfolioSidekick.Configuration;
using Microsoft.Extensions.Logging;
using Moq;

namespace GhostfolioSidekick.UnitTests
{
	public class DisplayInformationTaskTests
	{
		private readonly Mock<ILogger<DisplayInformationTask>> loggerMock;
		private readonly Mock<IApplicationSettings> applicationSettingsMock;
		private readonly DisplayInformationTask displayInformationTask;

		public DisplayInformationTaskTests()
		{
			loggerMock = new Mock<ILogger<DisplayInformationTask>>();
			applicationSettingsMock = new Mock<IApplicationSettings>();
			displayInformationTask = new DisplayInformationTask(loggerMock.Object, applicationSettingsMock.Object);
		}

		[Fact]
		public void DoWork_ShouldPrintUsedSettings_WithMappings()
		{
			// Arrange
			var settings = new ConfigurationInstance
			{
				Settings = new Settings
				{
					DataProviderPreference = "provider1",
					DeleteUnusedSymbols = true
				},
				Mappings = [new Mapping { MappingType = MappingType.Symbol, Source = "source1", Target = "target1" }]
			};

			applicationSettingsMock.Setup(x => x.ConfigurationInstance).Returns(settings);

			// Act
			displayInformationTask.DoWork();

			// Assert
			loggerMock.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Defined mappings: #1")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
			loggerMock.Verify(
		x => x.Log(
			LogLevel.Information,
			It.IsAny<EventId>(),
			It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Mapping Symbol: source1 -> target1")),
			It.IsAny<Exception>(),
			It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
		}

		[Fact]
		public void DoWork_ShouldPrintUsedSettings_WithoutMappings()
		{
			// Arrange
			var settings = new ConfigurationInstance
			{
				Settings = new Settings
				{
					DataProviderPreference = "provider1",
					DeleteUnusedSymbols = true
				},
			};

			applicationSettingsMock.Setup(x => x.ConfigurationInstance).Returns(settings);

			// Act
			displayInformationTask.DoWork();

			// Assert
			loggerMock.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Defined mappings: #0")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
		}

		[Fact]
		public void DoWork_ShouldPrintUsedSettings_WithDifferentSettings()
		{
			// Arrange
			var settings1 = new ConfigurationInstance
			{
				Settings = new Settings
				{
					DataProviderPreference = "provider1",
					DeleteUnusedSymbols = true
				},
				Mappings = [new Mapping { MappingType = MappingType.Symbol, Source = "source1", Target = "target1" }]
			};

			var settings2 = new ConfigurationInstance
			{
				Settings = new Settings
				{
					DataProviderPreference = "provider2",
					DeleteUnusedSymbols = false
				},
				Mappings = [new Mapping { MappingType = MappingType.Currency, Source = "source2", Target = "target2" }]
			};

			applicationSettingsMock.SetupSequence(x => x.ConfigurationInstance)
				.Returns(settings1)
				.Returns(settings2);

			// Act
			displayInformationTask.DoWork();
			displayInformationTask.DoWork();

			// Assert
			loggerMock.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("DataProviderPreference : provider1")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
			loggerMock.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("DataProviderPreference : provider2")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
		}

		[Fact]
		public void DoWork_ShouldPrintUsedSettings_WithDifferentMappings()
		{
			// Arrange
			var settings1 = new ConfigurationInstance
			{
				Settings = new Settings
				{
					DataProviderPreference = "provider1",
					DeleteUnusedSymbols = true
				},
				Mappings = [new Mapping { MappingType = MappingType.Symbol, Source = "source1", Target = "target1" }]
			};

			var settings2 = new ConfigurationInstance
			{
				Settings = new Settings
				{
					DataProviderPreference = "provider2",
					DeleteUnusedSymbols = false
				},
				Mappings = [new Mapping { MappingType = MappingType.Currency, Source = "source2", Target = "target2" }]
			};

			applicationSettingsMock.SetupSequence(x => x.ConfigurationInstance)
				.Returns(settings1)
				.Returns(settings2);

			// Act
			displayInformationTask.DoWork();
			displayInformationTask.DoWork();

			// Assert
			loggerMock.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Mapping Symbol: source1 -> target1")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
			loggerMock.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Mapping Currency: source2 -> target2")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
		}
	}
}
