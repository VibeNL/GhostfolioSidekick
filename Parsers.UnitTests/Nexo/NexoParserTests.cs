using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.Nexo;

namespace GhostfolioSidekick.Parsers.UnitTests.Nexo
{
	public class NexoParserTests
	{
		private readonly NexoParser parser;
		private readonly Account account;
		private readonly TestHoldingsCollection holdingsAndAccountsCollection;

		public NexoParserTests()
		{
			parser = new NexoParser(DummyCurrencyMapper.Instance);

			var fixture = new Fixture();
			account = fixture
				.Build<Account>()
				.With(x => x.Balance, new Balance(new Money(Currency.EUR, 0)))
				.Create();
			holdingsAndAccountsCollection = new TestHoldingsCollection(account);
		}

		[Fact]
		public async Task CanParseActivities_TestFiles_True()
		{
			// Arrange
			foreach (var file in Directory.GetFiles("./TestFiles/Nexo/", "*.csv", SearchOption.AllDirectories))
			{
				// Act
				var canParse = await parser.CanParseActivities(file);

				// Assert
				canParse.Should().BeTrue($"File {file}  cannot be parsed");
			}
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleDeposit_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Nexo/CashTransactions/single_deposit.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateCashDeposit(
						Currency.EUR,
						new DateTime(2023, 08, 25, 14, 44, 44, DateTimeKind.Utc),
						150,
						new Money(Currency.USD, 162.20249359M),
						"NXTM6EtqQukSs")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleWithdrawal_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Nexo/CashTransactions/single_withdrawal.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateCashWithdrawal(
						Currency.EUR,
						new DateTime(2024, 02, 28, 18, 34, 17, DateTimeKind.Utc),
						149.41000000M,
						new Money(Currency.USD, 161.96M),
						"NXT6UT3C1DP9h94hIqVrV3Wp9")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleBuy_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Nexo/BuyOrders/single_buy.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2023, 08, 25, 14, 44, 46, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateCrypto("USDC")],
						161.90485771M,
						0.9264700400075475907102725806M,
						new Money(Currency.USD, 161.9M),
						"NXTyPxhiopNL3")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleSell_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Nexo/SellOrders/single_sell.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateSell(
						Currency.EUR,
						new DateTime(2023, 08, 25, 14, 44, 46, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateCrypto("USDC")],
						161.90485771M,
						0.9264700400075475907102725806M,
						new Money(Currency.USD, 161.9M),
						"NXTyPxhiopNL3")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleConvert_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Nexo/BuyOrders/single_convert.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
					PartialActivity.CreateAssetConvert(
						new DateTime(2023, 10, 08, 19, 54, 20, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateCrypto("USDC")],
						200M,
						null,
						[PartialSymbolIdentifier.CreateCrypto("BTC")],
						0.00716057M,
						null,
						"NXTVDI4DJFWqB63pTcCuTpgc")
				);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleCashbackCrypto_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Nexo/Specials/single_cashback_crypto.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateGift(
						new DateTime(2023, 10, 12, 10, 44, 32, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateCrypto("BTC")],
						0.00000040M,
						"NXT2yQdOutpLLE1Lz51xXt6uW")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleCashbackFiat_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Nexo/Specials/single_cashback_fiat.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateGift(
						Currency.EUR,
						new DateTime(2023, 10, 8, 20, 5, 12, DateTimeKind.Utc),
						0.06548358M,
						new Money(Currency.USD, 0.069416M),
						"NXT6asbYnZqniNoTss0nyuIxM")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleReferralBonusPending_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Nexo/Specials/single_referralbonus_pending.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEmpty();
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleReferralBonusApproved_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Nexo/Specials/single_referralbonus_approved.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateGift(
						new DateTime(2023, 08, 25, 16, 43, 55, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateCrypto("BTC")],
						0.00096332M,
						"NXTk6FBYyxOqH")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleReceive_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Nexo/Receive/single_receive.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateReceive(
						new DateTime(2023, 12, 7, 19, 37, 32, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateCrypto("OP")],
						9.32835820M,
						"NXT53xOZQ1kJJOpfGPXe4RxWo")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleConvertFiatToFiat_Unsupported()
		{
			// Arrange

			// Act
			Func<Task> a = () => parser.ParseActivities("./TestFiles/Nexo/Invalid/fiat_to_fiat.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			await a.Should().ThrowAsync<NotSupportedException>();
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleInterest_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Nexo/CashTransactions/single_interest.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateInterest(
						Currency.EUR,
						new DateTime(2024, 01, 06, 06, 00, 00, DateTimeKind.Utc),
						0.00140202M,
						"Interest",
						new Money(Currency.USD, 0),
						"NXTeNtMHyjLigvrx7nFo8TT9")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleInterestFixedTerm_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Nexo/CashTransactions/single_interest_fixed_term.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateInterest(
						Currency.EUR,
						new DateTime(2024, 01, 06, 06, 00, 00, DateTimeKind.Utc),
						0.00140202M,
						"Fixed Term Interest",
						new Money(Currency.USD, 0),
						"NXTeNtMHyjLigvrx7nFo8TT9")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleInterestCrypto_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Nexo/Specials/single_interest_crypto.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateStakingReward(
						new DateTime(2024, 01, 10, 06, 00, 00, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateCrypto("BTC")],
						0.00000083M,
						"NXT4t20tunYP8Bjy5diZEOCk7")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_LockingFixTerm_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Nexo/Specials/single_lock_fix_term.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEmpty();
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_UnLockingFixTerm_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Nexo/Specials/single_unlock_fix_term.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEmpty();
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_InvalidType_ThrowsException()
		{
			// Arrange

			// Act
			Func<Task> a = () => parser.ParseActivities("./TestFiles/Nexo/Invalid/invalid_action.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			await a.Should().ThrowAsync<NotSupportedException>();
		}
	}
}