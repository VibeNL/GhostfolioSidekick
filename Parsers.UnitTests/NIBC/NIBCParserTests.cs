using AutoFixture;
using AwesomeAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.NIBC;

namespace GhostfolioSidekick.Parsers.UnitTests.NIBC
{
	public class NIBCParserTests
	{
		private readonly NIBCParser parser;
		private readonly Account account;
		private readonly TestActivityManager activityManager;

		public NIBCParserTests()
		{
			parser = new NIBCParser(DummyCurrencyMapper.Instance, TestParserSettings.Create(), TestParserSettings.Logger<NIBCParser>());

			var fixture = CustomFixture.New();
			account = fixture
				.Build<Account>()
				.With(x => x.Balance, [new Balance(DateOnly.FromDateTime(DateTime.Today), new Money(Currency.EUR, 0))])
				.Create();
			activityManager = new TestActivityManager();
		}

		[Fact]
		public async Task CanParseActivities_TestFiles_True()
		{
			// Arrange
			foreach (var file in Directory.GetFiles("./TestFiles/NIBC/", "*.csv", SearchOption.AllDirectories))
			{
				// Act
				var canParse = await parser.CanParse(file);

				// Assert
				canParse.Should().BeTrue($"File {file}  cannot be parsed");
			}
		}

		[Fact]
		public async Task CanParseActivities_QuotedAccountPreamble_True()
		{
			var contents = await File.ReadAllTextAsync("./TestFiles/NIBC/CashTransactions/single_deposit.csv", TestContext.Current.CancellationToken);
			contents = contents.Replace("Nr v/d rekening :;", "\"Nr v/d rekening :\";", StringComparison.Ordinal);
			var temporaryFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csv");
			await File.WriteAllTextAsync(temporaryFile, contents, TestContext.Current.CancellationToken);

			try
			{
				var canParse = await parser.CanParse(temporaryFile);

				canParse.Should().BeTrue();
			}
			finally
			{
				File.Delete(temporaryFile);
			}
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleDeposit_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/NIBC/CashTransactions/single_deposit.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateCashDeposit(
						Currency.EUR,
						new DateTime(2020, 01, 27, 0, 0, 0, 0, DateTimeKind.Utc),
						250M,
						new Money(Currency.EUR, 250M),
						"C0A27XM003000782")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_OpeningTermDeposit_Ignored()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/NIBC/CashTransactions/single_opening_term_deposit.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEmpty();
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_NegativeOpeningTermDeposit_Ignored()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/NIBC/CashTransactions/negative_opening_term_deposit.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEmpty();
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_NegativeIncomingTransfer_ThrowsNotSupportedException()
		{
			// Arrange

			// Act & Assert
			var ex = await Assert.ThrowsAsync<NotSupportedException>(() => parser.ParseActivities("./TestFiles/NIBC/CashTransactions/negative_incoming_transfer.csv", activityManager, account.Name));

			ex.Message.Should().Contain("Inkomende overboeking");
			ex.Message.Should().Contain("-500");
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleWithdrawal_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/NIBC/CashTransactions/single_withdrawal.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateCashWithdrawal(
						Currency.EUR,
						new DateTime(2022, 01, 06, 0, 0, 0, 0, DateTimeKind.Utc),
						10000M,
						new Money(Currency.EUR, 10000),
						"C2A06CW1G00A044D")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleInterest_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/NIBC/CashTransactions/single_interest.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateInterest(
						Currency.EUR,
						new DateTime(2021, 09, 30, 0, 0, 0, 0, DateTimeKind.Utc),
						0.51M,
						"Renteuitkering",
						new Money(Currency.EUR, 0.51M),
						"C1I30IN0000A000Q")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleBonusInterest_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/NIBC/CashTransactions/single_bonus_interest.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateInterest(
						Currency.EUR,
						new DateTime(2021, 6, 30, 0, 0, 0, DateTimeKind.Utc),
						1.1M,
						"Bonusrente",
						new Money(Currency.EUR, 1.1M),
						"C1F30IN0000A000Q")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_InvalidDescription_ThrowsUnknownTransactionTypeException()
		{
			// Arrange

			// Act & Assert
			var ex = await Assert.ThrowsAsync<UnknownTransactionTypeException>(() => parser.ParseActivities("./TestFiles/NIBC/Invalid/invalid_description.csv", activityManager, account.Name));

			ex.ParserName.Should().Be("NIBC");
			ex.TransactionType.Should().Be("Invalid");
		}
	}
}
