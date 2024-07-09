using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.Coinbase;

namespace GhostfolioSidekick.Parsers.UnitTests.Coinbase
{
	public class CoinbaseParserTests
	{
		private readonly CoinbaseParser parser;
		private readonly Account account;
		private readonly TestHoldingsCollection holdingsAndAccountsCollection;

		public CoinbaseParserTests()
		{
			parser = new CoinbaseParser(DummyCurrencyMapper.Instance);

			var fixture = new Fixture();
			account = fixture
				.Build<Account>()
				.With(x => x.Balance, new Balance(DateTime.Today, new Money(Currency.EUR, 0)))
				.Create();
			holdingsAndAccountsCollection = new TestHoldingsCollection(account);
		}

		[Fact]
		public async Task CanParseActivities_TestFiles_True()
		{
			// Arrange
			foreach (var file in Directory.GetFiles("./TestFiles/Coinbase/", "*.csv", SearchOption.AllDirectories))
			{
				// Act
				var canParse = await parser.CanParse(file);

				// Assert
				canParse.Should().BeTrue($"File {file}  cannot be parsed");
			}
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleDeposit_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Coinbase/CashTransactions/single_deposit.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateCashDeposit(
						Currency.EUR,
						new DateTime(2023, 12, 29, 20, 30, 38, DateTimeKind.Utc),
						10,
						new Money(Currency.EUR, 10),
						"Deposit_EUR_2023-12-29 20:30:38:+00:00")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleWithdrawal_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Coinbase/CashTransactions/single_withdrawal.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateCashWithdrawal(
						Currency.EUR,
						new DateTime(2023,11, 02, 09, 11, 11, DateTimeKind.Utc),
						10,
						new Money(Currency.EUR, 10),
						"Withdrawal_EUR_2023-11-02 09:11:11:+00:00")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleBuy_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Coinbase/BuyOrders/single_buy.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2023, 04, 20, 04, 05, 40, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateCrypto("ETH")],
						0.00213232M,
						1810.23M,
						new Money(Currency.EUR, 4.85M),
						"Buy_ETH_2023-04-20 04:05:40:+00:00"),
					PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2023, 04, 20, 04, 05, 40, DateTimeKind.Utc),
						0.99M,
						new Money(Currency.EUR, 0),
						"Buy_ETH_2023-04-20 04:05:40:+00:00"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleBuy_Alt_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Coinbase/BuyOrders/single_buy_alt.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2023, 04, 20, 04, 05, 40, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateCrypto("ETH")],
						0.00213232M,
						1810.23M,
						new Money(Currency.EUR, 4.85M),
						"6637ac0d7724c7009596c364"),
					PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2023, 04, 20, 04, 05, 40, DateTimeKind.Utc),
						0.99M,
						new Money(Currency.EUR, 0),
						"6637ac0d7724c7009596c364"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleAdvanceTradeBuy_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Coinbase/BuyOrders/single_advance_trade_buy.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2024, 03, 18, 11, 49, 37, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateCrypto("BTC")],
						0.564634M,
						100000.58M,
						new Money(Currency.EUR, 54321231.60M),
						"Advance Trade Buy_BTC_2024-03-18 11:49:37:+00:00"),
					PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2024, 03, 18, 11, 49, 37, DateTimeKind.Utc),
						0.99M,
						new Money(Currency.EUR, 0),
						"Advance Trade Buy_BTC_2024-03-18 11:49:37:+00:00"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleConvert_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Coinbase/BuyOrders/single_convert.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			var a = PartialActivity.CreateAssetConvert(
						new DateTime(2023, 04, 20, 04, 05, 40, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateCrypto("ETH")],
						0.00087766M,
						1709.09M,
						[PartialSymbolIdentifier.CreateCrypto("USDC")],
						1.629352M,
						null,
						"Convert_ETH_2023-04-20 04:05:40:+00:00").ToArray();
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					a[0],
					a[1],
					PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2023, 04, 20, 04, 05, 40, DateTimeKind.Utc),
						0.040000M,
						new Money(Currency.EUR, 0),
						"Convert_ETH_2023-04-20 04:05:40:+00:00"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleSell_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Coinbase/SellOrders/single_sell.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateSell(
						Currency.EUR,
						new DateTime(2023, 07, 14, 10, 40, 14, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateCrypto("USDC")],
						11.275271M,
						0.886900M,
						new Money(Currency.EUR, 10),
						"Sell_USDC_2023-07-14 10:40:14:+00:00")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleReceiveBuy_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Coinbase/Receive/single_receive.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateReceive(
						new DateTime(2023, 04, 22, 06, 24, 44, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateCrypto("ETH")],
						0.000000010M,
						"Receive_ETH_2023-04-22 06:24:44:+00:00")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleReceiveSend_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Coinbase/Send/single_send.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateSend(
						new DateTime(2023, 08, 19, 17, 23, 39, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateCrypto("BTC")],
						0.00205323M,
						"Send_BTC_2023-08-19 17:23:39:+00:00")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleStakeReward_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Coinbase/Specials/single_stakereward.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateStakingReward(
						new DateTime(2023, 5, 19, 18, 14, 56, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateCrypto("ETH2")],
						0.00002103M,
						"Rewards Income_ETH2_2023-05-19 18:14:56:+00:00")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleStakeReward_Alternative_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Coinbase/Specials/single_stakereward_alt.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateStakingReward(
						new DateTime(2023, 5, 19, 18, 14, 56, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateCrypto("ETH2")],
						0.00002103M,
						"Staking Income_ETH2_2023-05-19 18:14:56:+00:00")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleGift_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Coinbase/Specials/single_learningreward.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateGift(
						new DateTime(2023, 04, 20, 06, 02, 33, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateCrypto("GRT")],
						6.40204865M,
						"Learning Reward_GRT_2023-04-20 06:02:33:+00:00")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_InvalidType_ThrowsException()
		{
			// Arrange

			// Act
			Func<Task> a = async () => await parser.ParseActivities("./TestFiles/Coinbase/Invalid/invalid_type.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			await a.Should().ThrowAsync<NotSupportedException>();
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_BugCoinbase_BuyEurForEur_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Coinbase/Specials/single_buyfiatfromfiat_bugCoinbase.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEmpty();
		}
	}
}