using Microsoft.Extensions.Logging;
using Moq;

namespace GhostfolioSidekick.UnitTests
{
	public class TimedHostedServiceTests
	{
		[Fact]
		public async Task StartAsync_ShouldLogInformation()
		{
			// Arrange
			var loggerMock = new Mock<ILogger<TimedHostedService>>();
			var scheduledWorkMock = new Mock<IScheduledWork>();
			var service = new TimedHostedService(loggerMock.Object, new List<IScheduledWork> { scheduledWorkMock.Object });

			// Act
			await service.StartAsync(CancellationToken.None);

			// Assert
			loggerMock.Verify(logger => logger.Log(
				It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
				0,
				It.Is<It.IsAnyType>((@o, @t) => @o.ToString()!.StartsWith("Service is starting.")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
		}

		[Fact]
		public async Task DoWork_ShouldExecuteWorkItems()
		{
			// Arrange
			var loggerMock = new Mock<ILogger<TimedHostedService>>();
			var scheduledWorkMock1 = new Mock<IScheduledWork>();
			var scheduledWorkMock2 = new Mock<IScheduledWork>();
			var service = new TimedHostedService(loggerMock.Object, new List<IScheduledWork> { scheduledWorkMock1.Object, scheduledWorkMock2.Object });

			// Act
			await service.StartAsync(CancellationToken.None);
			await Task.Delay(100);

			// Assert
			scheduledWorkMock1.Verify(x => x.DoWork(), Times.Once);
			scheduledWorkMock2.Verify(x => x.DoWork(), Times.Once);
		}

		[Fact]
		public async Task StopAsync_ShouldLogInformation()
		{
			// Arrange
			var loggerMock = new Mock<ILogger<TimedHostedService>>();
			var scheduledWorkMock = new Mock<IScheduledWork>();
			var service = new TimedHostedService(loggerMock.Object, new List<IScheduledWork> { scheduledWorkMock.Object });

			// Act
			await service.StartAsync(CancellationToken.None);
			await service.StopAsync(CancellationToken.None);

			// Assert
			loggerMock.Verify(logger => logger.Log(
				It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
				0,
				It.Is<It.IsAnyType>((@o, @t) => @o.ToString()!.StartsWith("Service is stopping.")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
		}
	}
}
