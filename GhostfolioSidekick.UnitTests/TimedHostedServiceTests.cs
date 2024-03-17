using Microsoft.Extensions.Logging;
using Moq;

namespace GhostfolioSidekick.UnitTests
{
	public class TimedHostedServiceTests
	{
		[Fact]
		public async Task DoesNotStartAutomatically()
		{
			// Arrange
			var loggerMock = new Mock<ILogger<TimedHostedService>>();
			var scheduledWorkMock = new Mock<IScheduledWork>();
			new TimedHostedService(loggerMock.Object, new List<IScheduledWork> { scheduledWorkMock.Object });

			// Act
			await Task.Delay(1000);

			// Assert
			loggerMock.Verify(logger => logger.Log(
				It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
				0,
				It.Is<It.IsAnyType>((@o, @t) => @o.ToString()!.StartsWith("Service is starting.")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Never);
		}

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
			scheduledWorkMock1.Setup(x => x.ExecutionFrequency).Returns(TimeSpan.MaxValue);
			var scheduledWorkMock2 = new Mock<IScheduledWork>();
			scheduledWorkMock2.Setup(x => x.ExecutionFrequency).Returns(TimeSpan.MaxValue);
			var service = new TimedHostedService(loggerMock.Object, new List<IScheduledWork> { scheduledWorkMock1.Object, scheduledWorkMock2.Object });

			// Act
			await service.StartAsync(CancellationToken.None);
			await Task.Delay(100);

			// Assert
			scheduledWorkMock1.Verify(x => x.DoWork(), Times.Once);
			scheduledWorkMock2.Verify(x => x.DoWork(), Times.Once);
		}

		[Fact]
		public async Task DoWork_ShouldExecuteWorkItemsOnSchedule()
		{
			// Arrange
			var loggerMock = new Mock<ILogger<TimedHostedService>>();
			var scheduledWorkMock1 = new Mock<IScheduledWork>();
			scheduledWorkMock1.Setup(x => x.ExecutionFrequency).Returns(TimeSpan.FromMilliseconds(1));
			var scheduledWorkMock2 = new Mock<IScheduledWork>();
			scheduledWorkMock2.Setup(x => x.ExecutionFrequency).Returns(TimeSpan.FromSeconds(100));
			var service = new TimedHostedService(loggerMock.Object, new List<IScheduledWork> { scheduledWorkMock1.Object, scheduledWorkMock2.Object });

			// Act
			await service.StartAsync(CancellationToken.None);
			await Task.Delay(10000);

			// Assert
			scheduledWorkMock1.Verify(x => x.DoWork(), Times.AtLeast(5));
			scheduledWorkMock2.Verify(x => x.DoWork(), Times.Once);
		}

		[Fact]
		public async Task DoWork_ShouldExecuteWorkItemsOnSchedule_StopShouldWork()
		{
			// Arrange
			var loggerMock = new Mock<ILogger<TimedHostedService>>();
			var scheduledWorkMock1 = new Mock<IScheduledWork>();
			scheduledWorkMock1.Setup(x => x.ExecutionFrequency).Returns(TimeSpan.FromHours(1));
			var scheduledWorkMock2 = new Mock<IScheduledWork>();
			scheduledWorkMock2.Setup(x => x.ExecutionFrequency).Returns(TimeSpan.FromSeconds(5));
			var service = new TimedHostedService(loggerMock.Object, new List<IScheduledWork> { scheduledWorkMock1.Object, scheduledWorkMock2.Object });

			// Act
			await service.StartAsync(CancellationToken.None);
			await Task.Delay(100);
			await service.StopAsync(CancellationToken.None);

			// Assert
			scheduledWorkMock1.Verify(x => x.DoWork(), Times.Once());
			scheduledWorkMock2.Verify(x => x.DoWork(), Times.Once);
		}


		[Fact]
		public async Task DoWork_Exception_ShouldContinueToWork()
		{
			// Arrange
			var loggerMock = new Mock<ILogger<TimedHostedService>>();
			var scheduledWorkMock1 = new Mock<IScheduledWork>();
			scheduledWorkMock1.Setup(x => x.ExecutionFrequency).Returns(TimeSpan.MaxValue);
			scheduledWorkMock1.Setup(Task => Task.DoWork()).Throws(new Exception("Test exception"));
			var scheduledWorkMock2 = new Mock<IScheduledWork>();
			scheduledWorkMock2.Setup(x => x.ExecutionFrequency).Returns(TimeSpan.MaxValue);

			var service = new TimedHostedService(loggerMock.Object, new List<IScheduledWork> { scheduledWorkMock1.Object, scheduledWorkMock2.Object });

			// Act
			await service.StartAsync(CancellationToken.None);
			await Task.Delay(100);

			// Assert
			scheduledWorkMock1.Verify(x => x.DoWork(), Times.Once);
			scheduledWorkMock2.Verify(x => x.DoWork(), Times.Once);
			loggerMock.Verify(logger => logger.Log(
				It.Is<LogLevel>(logLevel => logLevel == LogLevel.Error),
				0,
				It.Is<It.IsAnyType>((@o, @t) => @o.ToString()!.StartsWith("Test exception")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
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
