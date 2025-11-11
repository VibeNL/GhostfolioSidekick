using AutoFixture;
using AwesomeAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.Trine;

namespace GhostfolioSidekick.Parsers.UnitTests.Trine
{
	public class TrineTests
	{
		private readonly TrineParser parser;
		private readonly Account account;
		private readonly TestActivityManager activityManager;

		public TrineTests()
		{
			parser = new TrineParser();

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
			foreach (var file in Directory.GetFiles("./TestFiles/Trine/", "*.csv", SearchOption.AllDirectories))
			{
				// Act
				var canParse = await parser.CanParse(file);

				// Assert
				canParse.Should().BeTrue($"File {file}  cannot be parsed");
			}
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleInvestment_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Trine/single_investment.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2023, 08, 29, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateGeneric("ecoligo 16")],
						1m,
						new Money(Currency.EUR, 25M),
						new Money(Currency.EUR, 25M),
						"?"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleRepayment_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Trine/single_repayment.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateDividend(
						Currency.EUR,
						new DateTime(2024, 05, 24, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateGeneric("ecoligo 16")],
						0.36M,
						new Money(Currency.EUR, 0.36M),
						"?"),
					PartialActivity.CreateSell(
						Currency.EUR,
						new DateTime(2024, 05, 24, 0, 0, 0, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateGeneric("ecoligo 16")],
						1m,
						new Money(Currency.EUR, 0.36M),
						new Money(Currency.EUR, 0.36M),
						"?"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleWithdrawal_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Trine/single_withdrawal.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateCashWithdrawal(
						Currency.EUR,
						new DateTime(2024, 06, 30, 0, 0, 0, DateTimeKind.Utc),
						1.32m,
						new Money(Currency.EUR, 1.32M),
						"?"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleFriendVoucher_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Trine/single_friend_voucher.csv", activityManager, account.Name);

			// Assert
			activityManager.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateGift(
						Currency.EUR,
						new DateTime(2023, 08, 22, 0, 0, 0, DateTimeKind.Utc),
						25m,
						new Money(Currency.EUR, 25M),
						"?"),
				]);
		}
	}
}
