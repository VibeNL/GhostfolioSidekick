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
		public void DoWork_ShouldPrintUsedSettings_Correctly()
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
			applicationSettingsMock.Setup(x => x.GhostfolioUrl).Returns("http://example.com");
			applicationSettingsMock.Setup(x => x.FileImporterPath).Returns("some_path");
			applicationSettingsMock.Setup(x => x.TrottleTimeout).Returns(TimeSpan.FromSeconds(30));

			// Act
			displayInformationTask.DoWork();

			// Assert
			loggerMock.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("GhostfolioUrl : http://example.com")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
			loggerMock.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("FileImporterPath : some_path")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
			loggerMock.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("TrottleTimeout : 00:00:30")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
		}
	}
}
