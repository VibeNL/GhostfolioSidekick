namespace GhostfolioSidekick.UnitTests
{
	public class TimedHostedServiceTests
	{
		private static DatabaseContext CreateInMemoryDatabaseContext()
		{
			var options = new DbContextOptionsBuilder<DatabaseContext>()
				.UseSqlite("Data Source=:memory:")
				.Options;

			var context = new DatabaseContext(options);
			// Need to open the connection to keep the in-memory database alive
			context.Database.OpenConnection();
			return context;
		}

		[Fact]
		public async Task DoesNotStartAutomatically()
		{
			// Arrange
			var loggerMock = new Mock<ILogger<TimedHostedService>>();
			using var databaseContext = CreateInMemoryDatabaseContext();
			var scheduledWorkMock = new Mock<IScheduledWork>();
			scheduledWorkMock.Setup(x => x.Name).Returns("Test");
			scheduledWorkMock.Setup(x => x.Priority).Returns(TaskPriority.DisplayInformation);
			scheduledWorkMock.Setup(x => x.ExecutionFrequency).Returns(TimeSpan.MaxValue);
			scheduledWorkMock.Setup(x => x.ExceptionsAreFatal).Returns(false);

			_ = new TimedHostedService(databaseContext, loggerMock.Object, new List<IScheduledWork> { scheduledWorkMock.Object });

			// Act
			await Task.Delay(1000, TestContext.Current.CancellationToken, TestContext.Current.CancellationToken);

			// Assert
			loggerMock.Verify(logger => logger.Log(
				It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
				It.IsAny<EventId>(),
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
			using var databaseContext = CreateInMemoryDatabaseContext();
			var scheduledWorkMock = new Mock<IScheduledWork>();
			scheduledWorkMock.Setup(x => x.Name).Returns("Test");
			scheduledWorkMock.Setup(x => x.Priority).Returns(TaskPriority.DisplayInformation);
			scheduledWorkMock.Setup(x => x.ExecutionFrequency).Returns(TimeSpan.MaxValue);
			scheduledWorkMock.Setup(x => x.ExceptionsAreFatal).Returns(false);

			var service = new TimedHostedService(databaseContext, loggerMock.Object, new List<IScheduledWork> { scheduledWorkMock.Object });

			// Act
			await service.StartAsync(CancellationToken.None);

			// Assert
			loggerMock.Verify(logger => logger.Log(
				It.Is<LogLevel>(logLevel => logLevel == LogLevel.Information),
				It.IsAny<EventId>(),
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
			using var databaseContext = CreateInMemoryDatabaseContext();
			var scheduledWorkMock1 = new Mock<IScheduledWork>();
			scheduledWorkMock1.Setup(x => x.Name).Returns("Test");
			scheduledWorkMock1.Setup(x => x.Priority).Returns(TaskPriority.DisplayInformation);
			scheduledWorkMock1.Setup(x => x.ExecutionFrequency).Returns(TimeSpan.MaxValue);
			scheduledWorkMock1.Setup(x => x.ExceptionsAreFatal).Returns(false);
			var scheduledWorkMock2 = new Mock<IScheduledWork>();
			scheduledWorkMock2.Setup(x => x.Name).Returns("Test2");
			scheduledWorkMock2.Setup(x => x.Priority).Returns(TaskPriority.AccountMaintainer);
			scheduledWorkMock2.Setup(x => x.ExecutionFrequency).Returns(TimeSpan.MaxValue);
			scheduledWorkMock2.Setup(x => x.ExceptionsAreFatal).Returns(false);

			var service = new TimedHostedService(databaseContext, loggerMock.Object, new List<IScheduledWork> { scheduledWorkMock1.Object, scheduledWorkMock2.Object });

			// Act
			await service.StartAsync(CancellationToken.None);
			await Task.Delay(100, TestContext.Current.CancellationToken, TestContext.Current.CancellationToken);

			// Assert
			scheduledWorkMock1.Verify(x => x.DoWork(It.IsAny<ILogger>()), Times.Once);
			scheduledWorkMock2.Verify(x => x.DoWork(It.IsAny<ILogger>()), Times.Once);
		}

		[Fact]
		public async Task DoWork_ShouldExecuteWorkItemsOnSchedule()
		{
			// Arrange
			var loggerMock = new Mock<ILogger<TimedHostedService>>();
			using var databaseContext = CreateInMemoryDatabaseContext();
			var scheduledWorkMock1 = new Mock<IScheduledWork>();
			scheduledWorkMock1.Setup(x => x.Name).Returns("Test");
			scheduledWorkMock1.Setup(x => x.Priority).Returns(TaskPriority.DisplayInformation);
			scheduledWorkMock1.Setup(x => x.ExecutionFrequency).Returns(TimeSpan.FromMilliseconds(1));
			scheduledWorkMock1.Setup(x => x.ExceptionsAreFatal).Returns(false);
			var scheduledWorkMock2 = new Mock<IScheduledWork>();
			scheduledWorkMock2.Setup(x => x.Name).Returns("Test");
			scheduledWorkMock2.Setup(x => x.Priority).Returns(TaskPriority.AccountMaintainer);
			scheduledWorkMock2.Setup(x => x.ExecutionFrequency).Returns(TimeSpan.FromSeconds(100));
			scheduledWorkMock2.Setup(x => x.ExceptionsAreFatal).Returns(false);

			var service = new TimedHostedService(databaseContext, loggerMock.Object, new List<IScheduledWork> { scheduledWorkMock1.Object, scheduledWorkMock2.Object });

			// Act
			await service.StartAsync(CancellationToken.None);
			await Task.Delay(500, TestContext.Current.CancellationToken, TestContext.Current.CancellationToken); // Reduced delay to make test faster

			// Assert
			scheduledWorkMock1.Verify(x => x.DoWork(It.IsAny<ILogger>()), Times.AtLeast(2)); // Should execute multiple times
			scheduledWorkMock2.Verify(x => x.DoWork(It.IsAny<ILogger>()), Times.Once); // Should execute once initially
		}

		[Fact]
		public async Task DoWork_ShouldExecuteWorkItemsOnSchedule_StopShouldWork()
		{
			// Arrange
			var loggerMock = new Mock<ILogger<TimedHostedService>>();
			using var databaseContext = CreateInMemoryDatabaseContext();
			var scheduledWorkMock1 = new Mock<IScheduledWork>();
			scheduledWorkMock1.Setup(x => x.Name).Returns("Test");
			scheduledWorkMock1.Setup(x => x.Priority).Returns(TaskPriority.DisplayInformation);
			scheduledWorkMock1.Setup(x => x.ExecutionFrequency).Returns(TimeSpan.FromHours(1));
			scheduledWorkMock1.Setup(x => x.ExceptionsAreFatal).Returns(false);
			var scheduledWorkMock2 = new Mock<IScheduledWork>();
			scheduledWorkMock2.Setup(x => x.Name).Returns("Test");
			scheduledWorkMock2.Setup(x => x.Priority).Returns(TaskPriority.AccountMaintainer);
			scheduledWorkMock2.Setup(x => x.ExecutionFrequency).Returns(TimeSpan.FromSeconds(5));
			scheduledWorkMock2.Setup(x => x.ExceptionsAreFatal).Returns(false);

			var service = new TimedHostedService(databaseContext, loggerMock.Object, new List<IScheduledWork> { scheduledWorkMock1.Object, scheduledWorkMock2.Object });

			// Act
			await service.StartAsync(CancellationToken.None);
			await Task.Delay(100, TestContext.Current.CancellationToken, TestContext.Current.CancellationToken);
			await service.StopAsync(CancellationToken.None);

			// Assert
			scheduledWorkMock1.Verify(x => x.DoWork(It.IsAny<ILogger>()), Times.Once());
			scheduledWorkMock2.Verify(x => x.DoWork(It.IsAny<ILogger>()), Times.Once);
		}

		[Fact]
		public async Task DoWork_Exception_ShouldContinueToWork()
		{
			// Arrange
			var loggerMock = new Mock<ILogger<TimedHostedService>>();
			using var databaseContext = CreateInMemoryDatabaseContext();
			var scheduledWorkMock1 = new Mock<IScheduledWork>();
			scheduledWorkMock1.Setup(x => x.Name).Returns("Test");
			scheduledWorkMock1.Setup(x => x.Priority).Returns(TaskPriority.DisplayInformation);
			scheduledWorkMock1.Setup(x => x.ExecutionFrequency).Returns(TimeSpan.MaxValue);
			scheduledWorkMock1.Setup(x => x.ExceptionsAreFatal).Returns(false);
			scheduledWorkMock1.Setup(Task => Task.DoWork(It.IsAny<ILogger>())).Throws(new Exception("Test exception42"));
			var scheduledWorkMock2 = new Mock<IScheduledWork>();
			scheduledWorkMock2.Setup(x => x.Name).Returns("Test");
			scheduledWorkMock2.Setup(x => x.Priority).Returns(TaskPriority.AccountMaintainer);
			scheduledWorkMock2.Setup(x => x.ExecutionFrequency).Returns(TimeSpan.MaxValue);
			scheduledWorkMock2.Setup(x => x.ExceptionsAreFatal).Returns(false);

			var service = new TimedHostedService(databaseContext, loggerMock.Object, new List<IScheduledWork> { scheduledWorkMock1.Object, scheduledWorkMock2.Object });

			// Act
			await service.StartAsync(CancellationToken.None);
			await Task.Delay(100, TestContext.Current.CancellationToken, TestContext.Current.CancellationToken);

			// Assert
			scheduledWorkMock1.Verify(x => x.DoWork(It.IsAny<ILogger>()), Times.Once);
			scheduledWorkMock2.Verify(x => x.DoWork(It.IsAny<ILogger>()), Times.Once);
			loggerMock.Verify(logger => logger.Log(
				It.Is<LogLevel>(logLevel => logLevel == LogLevel.Error),
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((@o, @t) => @o.ToString()!.Contains("Test exception42")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
		}

		[Fact]
		public async Task StopAsync_ShouldLogInformation()
		{
			// Arrange
			var loggerMock = new Mock<ILogger<TimedHostedService>>();
			using var databaseContext = CreateInMemoryDatabaseContext();
			var scheduledWorkMock = new Mock<IScheduledWork>();
			scheduledWorkMock.Setup(x => x.Name).Returns("Test");
			scheduledWorkMock.Setup(x => x.Priority).Returns(TaskPriority.DisplayInformation);
			scheduledWorkMock.Setup(x => x.ExecutionFrequency).Returns(TimeSpan.MaxValue);
			scheduledWorkMock.Setup(x => x.ExceptionsAreFatal).Returns(false);

			var service = new TimedHostedService(databaseContext, loggerMock.Object, new List<IScheduledWork> { scheduledWorkMock.Object });

			// Act
			await service.StartAsync(CancellationToken.None);
			await service.StopAsync(CancellationToken.None);

			// Assert
			loggerMock.Verify(logger => logger.Log(
				It.Is<LogLevel>(logLevel => logLevel == LogLevel.Debug),
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((@o, @t) => @o.ToString()!.StartsWith("Service is stopping.")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
		}
	}
}


