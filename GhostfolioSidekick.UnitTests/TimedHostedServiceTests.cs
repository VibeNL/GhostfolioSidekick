using AwesomeAssertions;
using xRetry.v3;

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
			await Task.Delay(1000, TestContext.Current.CancellationToken);

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

		[RetryFact]
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
			await Task.Delay(100, TestContext.Current.CancellationToken);

			// Assert
			scheduledWorkMock1.Verify(x => x.DoWork(It.IsAny<ILogger>()), Times.Once);
			scheduledWorkMock2.Verify(x => x.DoWork(It.IsAny<ILogger>()), Times.Once);
		}

		[RetryFact]
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
			await Task.Delay(500, TestContext.Current.CancellationToken); // Reduced delay to make test faster

			// Assert
			scheduledWorkMock1.Verify(x => x.DoWork(It.IsAny<ILogger>()), Times.AtLeast(2)); // Should execute multiple times
			scheduledWorkMock2.Verify(x => x.DoWork(It.IsAny<ILogger>()), Times.Once); // Should execute once initially
		}

		[RetryFact]
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
			await Task.Delay(100, TestContext.Current.CancellationToken);
			await service.StopAsync(CancellationToken.None);

			// Assert
			scheduledWorkMock1.Verify(x => x.DoWork(It.IsAny<ILogger>()), Times.Once());
			scheduledWorkMock2.Verify(x => x.DoWork(It.IsAny<ILogger>()), Times.Once);
		}

		[RetryFact]
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
			await Task.Delay(100, TestContext.Current.CancellationToken);

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
		public void Constructor_ShouldRemoveTaskTypes_ThatAreNoLongerRegistered()
		{
			// Arrange
			var loggerMock = new Mock<ILogger<TimedHostedService>>();
			using var databaseContext = CreateInMemoryDatabaseContext();

			var currentTaskMock = new Mock<IScheduledWork>();
			currentTaskMock.Setup(x => x.Name).Returns("Current Task");
			currentTaskMock.Setup(x => x.Priority).Returns(TaskPriority.DisplayInformation);
			currentTaskMock.Setup(x => x.ExecutionFrequency).Returns(TimeSpan.MaxValue);
			currentTaskMock.Setup(x => x.ExceptionsAreFatal).Returns(false);

			// First startup: initialises the schema and registers the current task
			_ = new TimedHostedService(databaseContext, loggerMock.Object,
				new List<IScheduledWork> { currentTaskMock.Object });

			// Simulate a stale row left over from a previous app version (e.g. MarketDataGathererNotOwnedTask)
			databaseContext.Tasks.Add(new Model.Tasks.TaskRun
			{
				Type = "MarketDataGathererNotOwnedTask",
				Name = "Removed Task",
				LastUpdate = DateTimeOffset.MinValue,
				Scheduled = true,
				InProgress = false
			});
			databaseContext.SaveChanges();
			databaseContext.Tasks.Count().Should().Be(2);

			// Act - second startup: removed task is absent from the registered work items
			_ = new TimedHostedService(databaseContext, loggerMock.Object,
				new List<IScheduledWork> { currentTaskMock.Object });

			// Assert - the orphaned row must be deleted entirely
			databaseContext.Tasks.Any(t => t.Type == "MarketDataGathererNotOwnedTask").Should().BeFalse();

			// The still-registered task must remain
			databaseContext.Tasks.Count().Should().Be(1);
			databaseContext.Tasks.Single().Scheduled.Should().BeTrue();
		}

		[Fact]
		public void Constructor_ShouldNotRemoveAnything_WhenAllTaskTypesAreStillRegistered()
		{
			// Arrange
			var loggerMock = new Mock<ILogger<TimedHostedService>>();
			using var databaseContext = CreateInMemoryDatabaseContext();

			var scheduledWorkMock = new Mock<IScheduledWork>();
			scheduledWorkMock.Setup(x => x.Name).Returns("Current Task");
			scheduledWorkMock.Setup(x => x.Priority).Returns(TaskPriority.DisplayInformation);
			scheduledWorkMock.Setup(x => x.ExecutionFrequency).Returns(TimeSpan.MaxValue);
			scheduledWorkMock.Setup(x => x.ExceptionsAreFatal).Returns(false);

			// Act
			_ = new TimedHostedService(databaseContext, loggerMock.Object, new List<IScheduledWork> { scheduledWorkMock.Object });

			// Assert - only one task row, and it is scheduled
			databaseContext.Tasks.Count().Should().Be(1);
			databaseContext.Tasks.Single().Scheduled.Should().BeTrue();

			// No removal log emitted
			loggerMock.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("no longer registered")),
					It.IsAny<Exception>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Never);
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


