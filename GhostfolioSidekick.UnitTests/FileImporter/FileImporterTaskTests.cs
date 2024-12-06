//using GhostfolioSidekick.Configuration;
//using GhostfolioSidekick.FileImporter;
//using GhostfolioSidekick.Parsers;
//using Microsoft.Extensions.Caching.Memory;
//using Microsoft.Extensions.Logging;
//using Moq;

//namespace GhostfolioSidekick.UnitTests.FileImporter
//{
//	public class FileImporterTaskTests
//	{
//		private readonly Mock<ILogger<FileImporterTask>> loggerMock;
//		private readonly Mock<IApplicationSettings> settingsMock;
//		private readonly Mock<IActivitiesService> activitiesManagerMock;
//		private readonly Mock<IAccountService> accountManagerMock;
//		private readonly Mock<IMarketDataService> marketDataManagerMock;
//		private readonly Mock<IExchangeRateService> exchangeRateServiceMock;
//		private readonly List<Mock<IFileImporter>> importersMock;
//		private readonly List<Mock<IHoldingStrategy>> strategiesMock;
//		private readonly IMemoryCache memoryCache;

//		public FileImporterTaskTests()
//		{
//			loggerMock = new Mock<ILogger<FileImporterTask>>();
//			settingsMock = new Mock<IApplicationSettings>();
//			activitiesManagerMock = new Mock<IActivitiesService>();
//			accountManagerMock = new Mock<IAccountService>();
//			marketDataManagerMock = new Mock<IMarketDataService>();
//			exchangeRateServiceMock = new Mock<IExchangeRateService>();
//			importersMock = [new Mock<IFileImporter>()];
//			strategiesMock = [new Mock<IHoldingStrategy>()];
//			memoryCache = new MemoryCache(new MemoryCacheOptions());
//		}

//		[Fact]
//		public async Task DoWork_NoFiles_ShouldNotDoWork()
//		{
//			// Arrange
//			settingsMock.Setup(x => x.FileImporterPath).Returns("FileImporter/testPath/acc1"); // Too deep :)

//			var fileImporterTask = new FileImporterTask(
//				loggerMock.Object,
//				settingsMock.Object,
//				activitiesManagerMock.Object,
//				accountManagerMock.Object,
//				marketDataManagerMock.Object,
//				exchangeRateServiceMock.Object,
//				importersMock.Select(x => x.Object),
//				strategiesMock.Select(x => x.Object),
//				memoryCache);

//			// Act
//			await fileImporterTask.DoWork();

//			// Assert
//			loggerMock.Verify(
//				x => x.Log(
//					LogLevel.Debug,
//					It.IsAny<EventId>(),
//					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Equals($"{nameof(FileImporterTask)} Skip to do work, no file changes detected")),
//					It.IsAny<Exception>(),
//					It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
//		}

//		[Fact]
//		public async Task DoWork_NoFileChanges_ShouldNotDoWork()
//		{
//			// Arrange
//			settingsMock.Setup(x => x.FileImporterPath).Returns("FileImporter/testPath");

//			var fileImporterTask = new FileImporterTask(
//				loggerMock.Object,
//				settingsMock.Object,
//				activitiesManagerMock.Object,
//				accountManagerMock.Object,
//				marketDataManagerMock.Object,
//				exchangeRateServiceMock.Object,
//				importersMock.Select(x => x.Object),
//				strategiesMock.Select(x => x.Object),
//				memoryCache);

//			memoryCache.Set(nameof(FileImporterTask), "3289fb407bc0f515a7c489dcda758c46eca6617dd8f7810ef3ac02ac8145b01be0defdcfd8042fa287cbbca983f3edc38ea083c41792ba6d471a2aeca26cf8d3");

//			// Act
//			await fileImporterTask.DoWork();

//			// Assert
//			loggerMock.Verify(
//				x => x.Log(
//					LogLevel.Debug,
//					It.IsAny<EventId>(),
//					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Equals($"{nameof(FileImporterTask)} Skip to do work, no file changes detected")),
//					It.IsAny<Exception>(),
//					It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
//		}

//		[Fact]
//		public async Task DoWork_FileChanges_ShouldDoWork()
//		{
//			// Arrange
//			settingsMock.Setup(x => x.FileImporterPath).Returns("FileImporter/testPath");

//			var fileImporterTask = new FileImporterTask(
//				loggerMock.Object,
//				settingsMock.Object,
//				activitiesManagerMock.Object,
//				accountManagerMock.Object,
//				marketDataManagerMock.Object,
//				exchangeRateServiceMock.Object,
//				importersMock.Select(x => x.Object),
//				strategiesMock.Select(x => x.Object),
//				memoryCache);

//			memoryCache.Set(nameof(FileImporterTask), "12");

//			// Act
//			await fileImporterTask.DoWork();

//			// Assert
//			loggerMock.Verify(
//				x => x.Log(
//					LogLevel.Debug,
//					It.IsAny<EventId>(),
//					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Equals($"{nameof(FileImporterTask)} Skip to do work, no file changes detected")),
//					It.IsAny<Exception>(),
//					It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);
//			loggerMock.Verify(
//				x => x.Log(
//					LogLevel.Debug,
//					It.IsAny<EventId>(),
//					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Equals($"{nameof(FileImporterTask)} Done")),
//					It.IsAny<Exception>(),
//					It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
//		}

//		[Fact]
//		public async Task DoWork_NoImporterAvailableException_ShouldLogError()
//		{
//			// Arrange
//			settingsMock.Setup(x => x.FileImporterPath).Returns("FileImporter/testPath");
//			importersMock[0].Setup(x => x.CanParse(It.IsAny<string>())).ReturnsAsync(false);

//			var fileImporterTask = new FileImporterTask(
//				loggerMock.Object,
//				settingsMock.Object,
//				activitiesManagerMock.Object,
//				accountManagerMock.Object,
//				marketDataManagerMock.Object,
//				exchangeRateServiceMock.Object,
//				importersMock.Select(x => x.Object),
//				strategiesMock.Select(x => x.Object),
//				memoryCache);

//			// Act
//			await fileImporterTask.DoWork();

//			// Assert
//			loggerMock.Verify(
//				x => x.Log(
//					LogLevel.Error,
//					It.IsAny<EventId>(),
//					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No importer available for")),
//					It.IsAny<Exception>(),
//					It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
//		}

//		[Fact]
//		public async Task DoWork_ExceptionThrown_ShouldLogError()
//		{
//			// Arrange
//			settingsMock.Setup(x => x.FileImporterPath).Returns("FileImporter/testPath");
//			importersMock[0].Setup(x => x.CanParse(It.IsAny<string>())).Throws(new Exception("Test Exception"));

//			var fileImporterTask = new FileImporterTask(
//				loggerMock.Object,
//				settingsMock.Object,
//				activitiesManagerMock.Object,
//				accountManagerMock.Object,
//				marketDataManagerMock.Object,
//				exchangeRateServiceMock.Object,
//				importersMock.Select(x => x.Object),
//				strategiesMock.Select(x => x.Object),
//				memoryCache);

//			// Act
//			await fileImporterTask.DoWork();

//			// Assert
//			loggerMock.Verify(
//				x => x.Log(
//					LogLevel.Error,
//					It.IsAny<EventId>(),
//					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Test Exception")),
//					It.IsAny<Exception>(),
//					It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
//		}

//	}
//}
