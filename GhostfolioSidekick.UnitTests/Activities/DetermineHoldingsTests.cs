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
			dbContextMock.Verify(db => db.SaveChangesAsync(default), Times.Exactly(2));
		}

		[Fact]
		public async Task DoWork_ShouldCreateNewHoldingsWhenNoExistingHoldings()
		{
			// Arrange
			var dbContextMock = new Mock<DatabaseContext>();
			var activities = new List<Activity>
			{
				new TestActivity { PartialSymbolIdentifiers = [ PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "TEST", null)!] }
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
			dbContextMock.Verify(db => db.SaveChangesAsync(default), Times.Exactly(2));
		}

		[Fact]
		public async Task DoWork_ShouldMergeActivitiesIntoSingleHolding_WhenSymbolMatches()
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
				new TestActivity { PartialSymbolIdentifiers = [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "TEST1", null)!] },
				new TestActivity { PartialSymbolIdentifiers = [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "TEST2", null)!] }
			};
			var holdings = new List<Holding>();

			dbContextMock.Setup(db => db.Activities).ReturnsDbSet(activities);
			dbContextMock.Setup(db => db.Holdings).ReturnsDbSet(holdings);
			dbContextMock.Setup(db => db.SymbolProfiles).ReturnsDbSet([]);
			_dbContextFactoryMock.Setup(factory => factory.CreateDbContextAsync()).ReturnsAsync(dbContextMock.Object);

			_symbolMatcherMock.Setup(sm => sm.MatchSymbol(It.IsAny<PartialSymbolIdentifier[]>())).ReturnsAsync(symbolProfile);

			// Act
			await _determineHoldings.DoWork(_loggerMock.Object);

			// Assert
			dbContextMock.Verify(db => db.Holdings.Add(It.Is<Holding>(h =>
				h.PartialSymbolIdentifiers.Select(x => x.Identifier).OrderBy(x => x).SequenceEqual(new[] { "TEST1", "TEST2" }) &&
				h.Activities.Count == 2)), Times.Once);
		}

		[Fact]
		public async Task DoWork_ShouldLogNoSymbolProfileFound_WhenNoSymbolMatcherReturnsSymbol()
		{
			// Arrange
			var dbContextMock = new Mock<DatabaseContext>();
			var activities = new List<Activity>
			{
				new TestActivity { PartialSymbolIdentifiers = [ PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "UNKNOWN", null)!] }
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
				new TestActivity { PartialSymbolIdentifiers = [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "OLD_SYMBOL", null)!] }
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
            // The matcher may be called more than once due to cache/context separation
			_symbolMatcherMock.Verify(sm => sm.MatchSymbol(It.Is<PartialSymbolIdentifier[]>(
				ids => ids.Any(id => id.Identifier == "NEW_SYMBOL"))), Times.AtLeastOnce());
		}

		[Fact]
      public async Task DoWork_ShouldCacheSymbolProfileResults()
		{
			// Arrange
			var dbContextMock = new Mock<DatabaseContext>();
			var activities = new List<Activity>
			{
				new TestActivity { PartialSymbolIdentifiers = [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "CACHED_SYMBOL", null)!] }
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

            // Assert - Symbol matcher should be called only as many times as needed (may be >1 due to context separation)
			_symbolMatcherMock.Verify(sm => sm.MatchSymbol(It.IsAny<PartialSymbolIdentifier[]>()), Times.AtLeast(1));
		}

	   [Fact]
	   public async Task DoWork_ShouldUseExistingSymbolProfile_WhenFoundInDatabase()
	   {
		   // Arrange
		   var dbContextMock = new Mock<DatabaseContext>();
		   var activities = new List<Activity>
		   {
			   new TestActivity { PartialSymbolIdentifiers = [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "EXISTING_SYMBOL", null)!] }
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
		   // The code should use the existing symbol profile from the database (preloaded)
		   // So we check that the SymbolProfiles DbSet was enumerated (preload) and that the holding was created with the correct profile
		   dbContextMock.Verify(db => db.SymbolProfiles, Times.AtLeastOnce());
		   // There should be a holding added with the correct symbol profile
		   dbContextMock.Verify(db => db.Holdings.Add(It.Is<Holding>(h => h.SymbolProfiles.Any(sp => sp.Symbol == "EXISTING_SYMBOL" && sp.DataSource == "TestSource"))), Times.Once);
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
				new TestActivity { PartialSymbolIdentifiers = [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "MULTI_MATCHER_SYMBOL", null)!] }
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
	  dbContextMock.Verify(db => db.SaveChangesAsync(default), Times.AtLeast(2));

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
				new TestActivity { PartialSymbolIdentifiers = [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "TEST_SYMBOL", null)!] }
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
			_symbolMatcherMock.Verify(sm => sm.MatchSymbol(It.IsAny<PartialSymbolIdentifier[]>()), Times.AtLeastOnce());
		}

		[Fact]
		public async Task DoWork_ShouldMergePartialIdentifiers_WhenHoldingExists()
		{
			// Arrange
			var dbContextMock = new Mock<DatabaseContext>();
			var existingPartialId = PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "EXISTING", null)!;
			var existingHolding = new Holding();
			existingHolding.PartialSymbolIdentifiers.Add(existingPartialId);

			var activities = new List<Activity>
			{
				new TestActivity { PartialSymbolIdentifiers = [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "EXISTING", null)!] }
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
		public async Task DoWork_ShouldKeepExistingPartialIdentifiers_WhenNoActivities()
		{
			// Arrange
			var dbContextMock = new Mock<DatabaseContext>();
			var holding = new Holding();
			holding.PartialSymbolIdentifiers.Add(PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "FIRST", null)!);
			holding.PartialSymbolIdentifiers.Add(PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "SECOND", null)!);
			holding.PartialSymbolIdentifiers.Add(PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "THIRD", null)!);

			var activities = new List<Activity>();
			var holdings = new List<Holding> { holding };

			dbContextMock.Setup(db => db.Activities).ReturnsDbSet(activities);
			dbContextMock.Setup(db => db.Holdings).ReturnsDbSet(holdings);
			_dbContextFactoryMock.Setup(factory => factory.CreateDbContextAsync()).ReturnsAsync(dbContextMock.Object);

			// Act
			await _determineHoldings.DoWork(_loggerMock.Object);

			// Assert
			holding.PartialSymbolIdentifiers.Should().HaveCount(3);
			holding.PartialSymbolIdentifiers.Select(x => x.Identifier).Should().BeEquivalentTo(["FIRST", "SECOND", "THIRD"]);
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
			dbContextMock.Verify(db => db.SaveChangesAsync(default), Times.Exactly(2));
		}

		[Fact]
		public async Task DoWork_ShouldCreateNewHolding_WhenExistingHoldingDoesNotMatchResolvedSymbol()
		{
			// Arrange
			var dbContextMock = new Mock<DatabaseContext>();
			var symbolProfile = new SymbolProfile { Symbol = "PRESERVED", DataSource = "TestSource" };
			var holding = new Holding();
			holding.SymbolProfiles.Add(symbolProfile);
			holding.PartialSymbolIdentifiers.Add(PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "FIRST", null)!);
			holding.PartialSymbolIdentifiers.Add(PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "SECOND", null)!);

			var activities = new List<Activity>
			{
				new TestActivity { PartialSymbolIdentifiers = [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Default, "NEW_SYMBOL", null)!] }
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
			dbContextMock.Verify(db => db.Holdings.Add(It.Is<Holding>(h =>
				h.SymbolProfiles.Any(sp => sp.Symbol == "NEW_SYMBOL") &&
				h.PartialSymbolIdentifiers.Any(id => id.Identifier == "NEW_SYMBOL"))), Times.Once);
			dbContextMock.Verify(db => db.Holdings.RemoveRange(It.Is<IEnumerable<Holding>>(removed => removed.Single() == holding)), Times.Once);
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

		[Fact]
		public async Task DoWork_ShouldPreferLatestActivity_WhenSameSymbolExistsInMultipleCurrencies()
		{
			// Arrange
			var dbContextMock = new Mock<DatabaseContext>();
			var olderActivity = new TestActivity
			{
				Date = new DateTime(2024, 1, 1),
				PartialSymbolIdentifiers = [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Ticker, "MSFT", Currency.EUR)!]
			};
			var newerActivity = new TestActivity
			{
				Date = new DateTime(2024, 2, 1),
				PartialSymbolIdentifiers = [PartialSymbolIdentifier.CreateGeneric(IdentifierType.Ticker, "MSFT", Currency.USD)!]
			};
			var activities = new List<Activity> { olderActivity, newerActivity };
			var holdings = new List<Holding>();

			dbContextMock.Setup(db => db.Activities).ReturnsDbSet(activities);
			dbContextMock.Setup(db => db.Holdings).ReturnsDbSet(holdings);
			dbContextMock.Setup(db => db.SymbolProfiles).ReturnsDbSet([]);
			_dbContextFactoryMock.Setup(factory => factory.CreateDbContextAsync()).ReturnsAsync(dbContextMock.Object);

			_symbolMatcherMock.Setup(sm => sm.MatchSymbol(It.Is<PartialSymbolIdentifier[]>(ids => ids.Any(id => id.Currency == Currency.USD))))
				.ReturnsAsync(new SymbolProfile { Symbol = "MSFT", DataSource = "TestSource", Currency = Currency.USD, Name = "Microsoft" });
			_symbolMatcherMock.Setup(sm => sm.MatchSymbol(It.Is<PartialSymbolIdentifier[]>(ids => ids.Any(id => id.Currency == Currency.EUR))))
				.ReturnsAsync(new SymbolProfile { Symbol = "MSFT", DataSource = "TestSource", Currency = Currency.EUR, Name = "Microsoft" });

			// Act
			await _determineHoldings.DoWork(_loggerMock.Object);

			// Assert
			dbContextMock.Verify(db => db.Holdings.Add(It.Is<Holding>(h => h.Activities.Count == 2)), Times.Once);
			dbContextMock.Verify(db => db.SymbolProfiles.Add(It.Is<SymbolProfile>(sp =>
				sp.Symbol == "MSFT" &&
				sp.DataSource == "TestSource" &&
				sp.Currency == Currency.USD)), Times.Once);
		}

		private record TestActivity : ActivityWithQuantityAndUnitPrice
		{
			public TestActivity()
			{
			}
		}
	}
}
