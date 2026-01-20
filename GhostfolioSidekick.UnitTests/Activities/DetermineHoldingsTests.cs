using AwesomeAssertions;
using GhostfolioSidekick.Activities;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.Extensions.Caching.Memory;
using Moq.EntityFrameworkCore;

namespace GhostfolioSidekick.UnitTests.Activities
{
	public class DetermineHoldingsTests
	{
		private readonly Mock<ILogger<DetermineHoldings>> _loggerMock;
		private readonly Mock<IDbContextFactory<DatabaseContext>> _dbContextFactoryMock;
		private readonly IMemoryCache _memoryCacheMock;
		private readonly Mock<ISymbolMatcher> _symbolMatcherMock;
		private readonly List<ISymbolMatcher> _symbolMatchers;
		private readonly DetermineHoldings _determineHoldings;

		public DetermineHoldingsTests()
		{
			_loggerMock = new Mock<ILogger<DetermineHoldings>>();
			_dbContextFactoryMock = new Mock<IDbContextFactory<DatabaseContext>>();
			_memoryCacheMock = new MemoryCache(new MemoryCacheOptions());
			_symbolMatcherMock = new Mock<ISymbolMatcher>();
			_symbolMatchers = [_symbolMatcherMock.Object];
			var _mockApplicationSettings = new Mock<IApplicationSettings>();

			_symbolMatcherMock.Setup(x => x.AllowedForDeterminingHolding).Returns(true);

			_determineHoldings = new DetermineHoldings(
				[.. _symbolMatchers],
				_dbContextFactoryMock.Object,
				_memoryCacheMock,
				_mockApplicationSettings.Object);
		}

		[Fact]
		public void Priority_ShouldReturnDetermineHoldings()
		{
			// Act
			var priority = _determineHoldings.Priority;

			// Assert
			priority.Should().Be(TaskPriority.DetermineHoldings);
		}

		[Fact]
		public void ExecutionFrequency_ShouldReturnHourly()
		{
			// Act
			var frequency = _determineHoldings.ExecutionFrequency;

			// Assert
			frequency.Should().Be(Frequencies.Hourly);
		}

		[Fact]
		public void ExceptionsAreFatal_ShouldReturnFalse()
		{
			// Act
			var exceptionsAreFatal = _determineHoldings.ExceptionsAreFatal;

			// Assert
			exceptionsAreFatal.Should().BeFalse();
		}

		[Fact]
		public async Task DoWork_ShouldRemoveUnusedHoldings()
		{
			// Arrange
			var dbContextMock = new Mock<DatabaseContext>();
			var activities = new List<Activity>(); // No activities means all holdings are unused
			var holdings = new List<Holding>
			{
				new() { Id = 1 },
				new() { Id = 2 }
			};

			dbContextMock.Setup(db => db.Activities).ReturnsDbSet(activities);
			dbContextMock.Setup(db => db.Holdings).ReturnsDbSet(holdings);
			_dbContextFactoryMock.Setup(factory => factory.CreateDbContextAsync()).ReturnsAsync(dbContextMock.Object);

			var loggerMock = new Mock<ILogger<DetermineHoldings>>();

			// Act
			await _determineHoldings.DoWork(loggerMock.Object);

			// Assert
			dbContextMock.Verify(db => db.Holdings.RemoveRange(It.Is<IEnumerable<Holding>>(h => h.Count() == 2)), Times.Once);
			// SaveChangesAsync called twice: once for clearing existing holdings, once at the end
			dbContextMock.Verify(db => db.SaveChangesAsync(default), Times.Exactly(2));
		}

		[Fact]
		public async Task DoWork_ShouldCreateNewHoldingsWhenNoExistingHoldings()
		{
			// Arrange
			var dbContextMock = new Mock<DatabaseContext>();
			var activities = new List<Activity>
			{
				new TestActivity { PartialSymbolIdentifiers = [ PartialSymbolIdentifier.CreateGeneric("TEST")] }
			};
			var holdings = new List<Holding>(); // No existing holdings to reuse

			dbContextMock.Setup(db => db.Activities).ReturnsDbSet(activities);
			dbContextMock.Setup(db => db.Holdings).ReturnsDbSet(holdings);
			dbContextMock.Setup(db => db.SymbolProfiles).ReturnsDbSet([]);
			_dbContextFactoryMock.Setup(factory => factory.CreateDbContextAsync()).ReturnsAsync(dbContextMock.Object);

			_symbolMatcherMock.Setup(sm => sm.MatchSymbol(It.IsAny<PartialSymbolIdentifier[]>())).ReturnsAsync(new SymbolProfile { Symbol = "TEST", DataSource = "TestSource" });

			var loggerMock = new Mock<ILogger<DetermineHoldings>>();

			// Act
			await _determineHoldings.DoWork(loggerMock.Object);

			// Assert
			dbContextMock.Verify(db => db.Holdings.Add(It.IsAny<Holding>()), Times.Once);
			// SaveChangesAsync called twice: once for clearing existing holdings, once at the end
			dbContextMock.Verify(db => db.SaveChangesAsync(default), Times.Exactly(2));
		}

		[Fact]
		public async Task DoWork_ShouldLogHoldingAlreadyExistsForSymbol_WhenSymbolHoldingDictionaryContainsSymbol()
		{
			// Arrange
			var dbContextMock = new Mock<DatabaseContext>();
			var symbolProfile = new SymbolProfile
			{
				Symbol = "TEST",
				DataSource = "TestSource",
				Currency = Currency.USD,
				Name = "Test Symbol"
			};

			var activities = new List<Activity>
			{
				new TestActivity { PartialSymbolIdentifiers = [ PartialSymbolIdentifier.CreateGeneric("TEST1")] },
				new TestActivity { PartialSymbolIdentifiers = [ PartialSymbolIdentifier.CreateGeneric("TEST2")] }
			};
			var holdings = new List<Holding>();

			dbContextMock.Setup(db => db.Activities).ReturnsDbSet(activities);
			dbContextMock.Setup(db => db.Holdings).ReturnsDbSet(holdings);
			dbContextMock.Setup(db => db.SymbolProfiles).ReturnsDbSet([]);
			_dbContextFactoryMock.Setup(factory => factory.CreateDbContextAsync()).ReturnsAsync(dbContextMock.Object);

			// Return the same symbol profile for both partial identifiers
			_symbolMatcherMock.Setup(sm => sm.MatchSymbol(It.IsAny<PartialSymbolIdentifier[]>())).ReturnsAsync(symbolProfile);

			// Act
			await _determineHoldings.DoWork(_loggerMock.Object);

			// Assert
			// Verify that the log message for "holding already exists for symbol" was called
			_loggerMock.Verify(
				x => x.Log(
					LogLevel.Trace,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CreateOrReuseHolding: Merging identifiers for existing holding with symbol") &&
										v.ToString()!.Contains("TEST")),
					It.IsAny<Exception?>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task DoWork_ShouldLogNoSymbolProfileFound_WhenNoSymbolMatcherReturnsSymbol()
		{
			// Arrange
			var dbContextMock = new Mock<DatabaseContext>();
			var activities = new List<Activity>
			{
				new TestActivity { PartialSymbolIdentifiers = [ PartialSymbolIdentifier.CreateGeneric("UNKNOWN")] }
			};
			var holdings = new List<Holding>();

			dbContextMock.Setup(db => db.Activities).ReturnsDbSet(activities);
			dbContextMock.Setup(db => db.Holdings).ReturnsDbSet(holdings);
			dbContextMock.Setup(db => db.SymbolProfiles).ReturnsDbSet([]);
			_dbContextFactoryMock.Setup(factory => factory.CreateDbContextAsync()).ReturnsAsync(dbContextMock.Object);

			// Return null for all symbol matches
			_symbolMatcherMock.Setup(sm => sm.MatchSymbol(It.IsAny<PartialSymbolIdentifier[]>())).ReturnsAsync((SymbolProfile?)null);

			// Act
			await _determineHoldings.DoWork(_loggerMock.Object);

			// Assert
			// Verify that the log warning for "no symbol profile found" was called
			_loggerMock.Verify(
				x => x.Log(
					LogLevel.Warning,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("CreateOrReuseHolding: No symbol profile found for") &&
										v.ToString()!.Contains("UNKNOWN")),
					It.IsAny<Exception?>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		[Fact]
		public async Task DoWork_ShouldApplyMappings_WhenMappingsConfigured()
		{
			// Arrange
			var mappings = new[]
			{
				new Mapping { Source = "OLD_SYMBOL", Target = "NEW_SYMBOL", MappingType = MappingType.Symbol }
			};
			var configurationInstance = new ConfigurationInstance
			{
				Mappings = mappings
			};
			var applicationSettings = new Mock<IApplicationSettings>();
			applicationSettings.Setup(x => x.ConfigurationInstance).Returns(configurationInstance);

			var determineHoldingsWithMappings = new DetermineHoldings(
				[.. _symbolMatchers],
				_dbContextFactoryMock.Object,
				_memoryCacheMock,
				applicationSettings.Object);

			var dbContextMock = new Mock<DatabaseContext>();
			var activities = new List<Activity>
			{
				new TestActivity { PartialSymbolIdentifiers = [PartialSymbolIdentifier.CreateGeneric("OLD_SYMBOL")] }
			};
			var holdings = new List<Holding>();

			dbContextMock.Setup(db => db.Activities).ReturnsDbSet(activities);
			dbContextMock.Setup(db => db.Holdings).ReturnsDbSet(holdings);
			dbContextMock.Setup(db => db.SymbolProfiles).ReturnsDbSet([]);
			_dbContextFactoryMock.Setup(factory => factory.CreateDbContextAsync()).ReturnsAsync(dbContextMock.Object);

			_symbolMatcherMock.Setup(sm => sm.MatchSymbol(It.Is<PartialSymbolIdentifier[]>(
				ids => ids.Any(id => id.Identifier == "NEW_SYMBOL"))))
				.ReturnsAsync(new SymbolProfile { Symbol = "NEW_SYMBOL", DataSource = "TestSource" });

			// Act
			await determineHoldingsWithMappings.DoWork(_loggerMock.Object);

			// Assert
			_symbolMatcherMock.Verify(sm => sm.MatchSymbol(It.Is<PartialSymbolIdentifier[]>(
				ids => ids.Any(id => id.Identifier == "NEW_SYMBOL"))), Times.Once);
		}

		[Fact]
		public async Task DoWork_ShouldCacheSymbolProfileResults()
		{
			// Arrange
			var dbContextMock = new Mock<DatabaseContext>();
			var activities = new List<Activity>
			{
				new TestActivity { PartialSymbolIdentifiers = [PartialSymbolIdentifier.CreateGeneric("CACHED_SYMBOL")] }
			};
			var holdings = new List<Holding>();

			dbContextMock.Setup(db => db.Activities).ReturnsDbSet(activities);
			dbContextMock.Setup(db => db.Holdings).ReturnsDbSet(holdings);
			dbContextMock.Setup(db => db.SymbolProfiles).ReturnsDbSet([]);
			_dbContextFactoryMock.Setup(factory => factory.CreateDbContextAsync()).ReturnsAsync(dbContextMock.Object);

			var symbolProfile = new SymbolProfile { Symbol = "CACHED_SYMBOL", DataSource = "TestSource" };
			_symbolMatcherMock.Setup(sm => sm.MatchSymbol(It.IsAny<PartialSymbolIdentifier[]>()))
				.ReturnsAsync(symbolProfile);

			// Act - First call
			await _determineHoldings.DoWork(_loggerMock.Object);
			
			// Reset dbContextMock for second call
			dbContextMock.Invocations.Clear();
			_dbContextFactoryMock.Setup(factory => factory.CreateDbContextAsync()).ReturnsAsync(dbContextMock.Object);

			// Act - Second call (should use cache)
			await _determineHoldings.DoWork(_loggerMock.Object);

			// Assert - Symbol matcher should be called only once due to caching
			_symbolMatcherMock.Verify(sm => sm.MatchSymbol(It.IsAny<PartialSymbolIdentifier[]>()), Times.Once);
		}

		[Fact]
		public async Task DoWork_ShouldUseExistingSymbolProfile_WhenFoundInDatabase()
		{
			// Arrange
			var dbContextMock = new Mock<DatabaseContext>();
			var activities = new List<Activity>
			{
				new TestActivity { PartialSymbolIdentifiers = [PartialSymbolIdentifier.CreateGeneric("EXISTING_SYMBOL")] }
			};
			var holdings = new List<Holding>();
			var existingSymbolProfile = new SymbolProfile 
			{ 
				Symbol = "EXISTING_SYMBOL", 
				DataSource = "TestSource",
				Currency = Currency.USD,
				Name = "Existing Symbol"
			};

			dbContextMock.Setup(db => db.Activities).ReturnsDbSet(activities);
			dbContextMock.Setup(db => db.Holdings).ReturnsDbSet(holdings);
			dbContextMock.Setup(db => db.SymbolProfiles).ReturnsDbSet([existingSymbolProfile]);
			_dbContextFactoryMock.Setup(factory => factory.CreateDbContextAsync()).ReturnsAsync(dbContextMock.Object);

			_symbolMatcherMock.Setup(sm => sm.MatchSymbol(It.IsAny<PartialSymbolIdentifier[]>()))
				.ReturnsAsync(new SymbolProfile { Symbol = "EXISTING_SYMBOL", DataSource = "TestSource" });

			// Act
			await _determineHoldings.DoWork(_loggerMock.Object);

			// Assert
			dbContextMock.Verify(db => db.Holdings.Add(It.Is<Holding>(h => 
				h.SymbolProfiles.Contains(existingSymbolProfile))), Times.Once);
		}

		[Fact]
		public async Task DoWork_ShouldHandleMultipleSymbolMatchers()
		{
			// Arrange
			var firstSymbolMatcher = new Mock<ISymbolMatcher>();
			firstSymbolMatcher.Setup(x => x.AllowedForDeterminingHolding).Returns(true);
			
			var symbolMatcher2 = new Mock<ISymbolMatcher>();
			symbolMatcher2.Setup(x => x.AllowedForDeterminingHolding).Returns(true);

			var determineHoldingsMultipleMatchers = new DetermineHoldings(
				[firstSymbolMatcher.Object, symbolMatcher2.Object],
				_dbContextFactoryMock.Object,
				_memoryCacheMock,
				new Mock<IApplicationSettings>().Object);

			var dbContextMock = new Mock<DatabaseContext>();
			var activities = new List<Activity>
			{
				new TestActivity { PartialSymbolIdentifiers = [PartialSymbolIdentifier.CreateGeneric("MULTI_MATCHER_SYMBOL")] }
			};
			var holdings = new List<Holding>();

			dbContextMock.Setup(db => db.Activities).ReturnsDbSet(activities);
			dbContextMock.Setup(db => db.Holdings).ReturnsDbSet(holdings);
			dbContextMock.Setup(db => db.SymbolProfiles).ReturnsDbSet([]);
			_dbContextFactoryMock.Setup(factory => factory.CreateDbContextAsync()).ReturnsAsync(dbContextMock.Object);

			// First matcher returns null, second returns symbol
			firstSymbolMatcher.Setup(sm => sm.MatchSymbol(It.IsAny<PartialSymbolIdentifier[]>()))
				.ReturnsAsync((SymbolProfile?)null);
			
			symbolMatcher2.Setup(sm => sm.MatchSymbol(It.IsAny<PartialSymbolIdentifier[]>()))
				.ReturnsAsync(new SymbolProfile { Symbol = "MULTI_MATCHER_SYMBOL", DataSource = "SecondSource" });

			// Act
			await determineHoldingsMultipleMatchers.DoWork(_loggerMock.Object);

			// Assert
		// Verify that both matchers had their AllowedForDeterminingHolding property checked (actual MatchSymbol calls depend on implementation logic)
		// Verify the method completes successfully
		dbContextMock.Verify(db => db.SaveChangesAsync(default), Times.Exactly(2));
		
		
		// Verify that both matchers had their AllowedForDeterminingHolding property checked
		firstSymbolMatcher.VerifyGet(x => x.AllowedForDeterminingHolding, Times.AtLeastOnce);
		symbolMatcher2.VerifyGet(x => x.AllowedForDeterminingHolding, Times.AtLeastOnce);
	}

		[Fact]
		public async Task DoWork_ShouldSkipDisallowedSymbolMatchers()
		{
			// Arrange
			var disallowedMatcher = new Mock<ISymbolMatcher>();
			disallowedMatcher.Setup(x => x.AllowedForDeterminingHolding).Returns(false);

			var determineHoldingsWithDisallowed = new DetermineHoldings(
				[disallowedMatcher.Object, _symbolMatcherMock.Object],
				_dbContextFactoryMock.Object,
				_memoryCacheMock,
				new Mock<IApplicationSettings>().Object);

			var dbContextMock = new Mock<DatabaseContext>();
			var activities = new List<Activity>
			{
				new TestActivity { PartialSymbolIdentifiers = [PartialSymbolIdentifier.CreateGeneric("TEST_SYMBOL")] }
			};
			var holdings = new List<Holding>();

			dbContextMock.Setup(db => db.Activities).ReturnsDbSet(activities);
			dbContextMock.Setup(db => db.Holdings).ReturnsDbSet(holdings);
			dbContextMock.Setup(db => db.SymbolProfiles).ReturnsDbSet([]);
			_dbContextFactoryMock.Setup(factory => factory.CreateDbContextAsync()).ReturnsAsync(dbContextMock.Object);

			_symbolMatcherMock.Setup(sm => sm.MatchSymbol(It.IsAny<PartialSymbolIdentifier[]>()))
				.ReturnsAsync(new SymbolProfile { Symbol = "TEST_SYMBOL", DataSource = "AllowedSource" });

			// Act
			await determineHoldingsWithDisallowed.DoWork(_loggerMock.Object);

			// Assert
			disallowedMatcher.Verify(sm => sm.MatchSymbol(It.IsAny<PartialSymbolIdentifier[]>()), Times.Never);
			_symbolMatcherMock.Verify(sm => sm.MatchSymbol(It.IsAny<PartialSymbolIdentifier[]>()), Times.Once);
		}

		[Fact]
		public async Task DoWork_ShouldMergePartialIdentifiers_WhenHoldingExists()
		{
			// Arrange
			var dbContextMock = new Mock<DatabaseContext>();
			var existingPartialId = PartialSymbolIdentifier.CreateGeneric("EXISTING");
			var existingHolding = new Holding();
			existingHolding.PartialSymbolIdentifiers.Add(existingPartialId);

			var activities = new List<Activity>
			{
				new TestActivity { PartialSymbolIdentifiers = [PartialSymbolIdentifier.CreateGeneric("EXISTING")] }
			};
			var holdings = new List<Holding> { existingHolding };

			dbContextMock.Setup(db => db.Activities).ReturnsDbSet(activities);
			dbContextMock.Setup(db => db.Holdings).ReturnsDbSet(holdings);
			dbContextMock.Setup(db => db.SymbolProfiles).ReturnsDbSet([]);
			_dbContextFactoryMock.Setup(factory => factory.CreateDbContextAsync()).ReturnsAsync(dbContextMock.Object);

			_symbolMatcherMock.Setup(sm => sm.MatchSymbol(It.IsAny<PartialSymbolIdentifier[]>()))
				.ReturnsAsync(new SymbolProfile { Symbol = "EXISTING", DataSource = "TestSource" });

			// Act
			await _determineHoldings.DoWork(_loggerMock.Object);

			// Assert
			existingHolding.SymbolProfiles.Should().HaveCount(1);
			existingHolding.PartialSymbolIdentifiers.Should().Contain(x => x.Identifier == "EXISTING");
		}

		[Fact]
		public async Task DoWork_ShouldClearAllPartialIdentifiers_WhenNoActivities()
		{
			// Arrange
			var dbContextMock = new Mock<DatabaseContext>();
			var holding = new Holding();
			holding.PartialSymbolIdentifiers.Add(PartialSymbolIdentifier.CreateGeneric("FIRST"));
			holding.PartialSymbolIdentifiers.Add(PartialSymbolIdentifier.CreateGeneric("SECOND"));
			holding.PartialSymbolIdentifiers.Add(PartialSymbolIdentifier.CreateGeneric("THIRD"));

			var activities = new List<Activity>();
			var holdings = new List<Holding> { holding };

			dbContextMock.Setup(db => db.Activities).ReturnsDbSet(activities);
			dbContextMock.Setup(db => db.Holdings).ReturnsDbSet(holdings);
			_dbContextFactoryMock.Setup(factory => factory.CreateDbContextAsync()).ReturnsAsync(dbContextMock.Object);

			// Act
			await _determineHoldings.DoWork(_loggerMock.Object);

			// Assert
			holding.PartialSymbolIdentifiers.Should().BeEmpty();
			
			
		}

		[Fact]
		public async Task DoWork_ShouldHandleEmptyActivities()
		{
			// Arrange
			var dbContextMock = new Mock<DatabaseContext>();
			var activities = new List<Activity>();
			var holdings = new List<Holding>();

			dbContextMock.Setup(db => db.Activities).ReturnsDbSet(activities);
			dbContextMock.Setup(db => db.Holdings).ReturnsDbSet(holdings);
			_dbContextFactoryMock.Setup(factory => factory.CreateDbContextAsync()).ReturnsAsync(dbContextMock.Object);

			// Act
			await _determineHoldings.DoWork(_loggerMock.Object);

			// Assert
			dbContextMock.Verify(db => db.Holdings.Add(It.IsAny<Holding>()), Times.Never);
			dbContextMock.Verify(db => db.Holdings.Remove(It.IsAny<Holding>()), Times.Never);
			// SaveChangesAsync called twice: once for clearing existing holdings, once at the end
			dbContextMock.Verify(db => db.SaveChangesAsync(default), Times.Exactly(2));
		}

		[Fact]
		public async Task DoWork_ShouldReuseExistingHoldingForNewSymbol()
		{
			// Arrange
			var dbContextMock = new Mock<DatabaseContext>();
			var symbolProfile = new SymbolProfile { Symbol = "PRESERVED", DataSource = "TestSource" };
			var holding = new Holding();
			holding.SymbolProfiles.Add(symbolProfile);
			holding.PartialSymbolIdentifiers.Add(PartialSymbolIdentifier.CreateGeneric("FIRST"));
			holding.PartialSymbolIdentifiers.Add(PartialSymbolIdentifier.CreateGeneric("SECOND"));

			var activities = new List<Activity>
			{
				new TestActivity { PartialSymbolIdentifiers = [PartialSymbolIdentifier.CreateGeneric("NEW_SYMBOL")] }
			};
			var holdings = new List<Holding> { holding };

			dbContextMock.Setup(db => db.Activities).ReturnsDbSet(activities);
			dbContextMock.Setup(db => db.Holdings).ReturnsDbSet(holdings);
			dbContextMock.Setup(db => db.SymbolProfiles).ReturnsDbSet([]);
			_dbContextFactoryMock.Setup(factory => factory.CreateDbContextAsync()).ReturnsAsync(dbContextMock.Object);

			_symbolMatcherMock.Setup(sm => sm.MatchSymbol(It.IsAny<PartialSymbolIdentifier[]>()))
				.ReturnsAsync(new SymbolProfile { Symbol = "NEW_SYMBOL", DataSource = "TestSource" });

			// Act
			await _determineHoldings.DoWork(_loggerMock.Object);

			// Assert
		// The existing holding should be reused for the new symbol
		holding.SymbolProfiles.Should().HaveCount(1);
		holding.SymbolProfiles[0].Symbol.Should().Be("NEW_SYMBOL");
		holding.PartialSymbolIdentifiers.Should().HaveCount(1);
		holding.PartialSymbolIdentifiers[0].Identifier.Should().Be("NEW_SYMBOL");
			
			
		}

		[Fact]
		public async Task DoWork_ShouldLogInformation_WhenRemovingUnusedHoldings()
		{
			// Arrange
			var dbContextMock = new Mock<DatabaseContext>();
			var activities = new List<Activity>();
			var holdingToRemove = new Holding { Id = 42 };
			var holdings = new List<Holding> { holdingToRemove };

			dbContextMock.Setup(db => db.Activities).ReturnsDbSet(activities);
			dbContextMock.Setup(db => db.Holdings).ReturnsDbSet(holdings);
			_dbContextFactoryMock.Setup(factory => factory.CreateDbContextAsync()).ReturnsAsync(dbContextMock.Object);

			// Act
			await _determineHoldings.DoWork(_loggerMock.Object);

			// Assert
			_loggerMock.Verify(
				x => x.Log(
					LogLevel.Information,
					It.IsAny<EventId>(),
					It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Removing") &&
										v.ToString()!.Contains("unused holdings")),
					It.IsAny<Exception?>(),
					It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
				Times.Once);
		}

		private record TestActivity : ActivityWithQuantityAndUnitPrice
		{
			public TestActivity()
			{
			}
		}
	}
}
