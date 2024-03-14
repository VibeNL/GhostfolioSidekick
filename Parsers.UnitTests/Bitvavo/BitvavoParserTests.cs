using AutoFixture;
using FluentAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.Bitvavo;

namespace GhostfolioSidekick.Parsers.UnitTests.Bitvavo
{
	public class BitvavoParserTests
	{
		private readonly BitvavoParser parser;
		private readonly Account account;
		private readonly TestHoldingsCollection holdingsAndAccountsCollection;

		public BitvavoParserTests()
		{
			parser = new BitvavoParser(DummyCurrencyMapper.Instance);

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
			foreach (var file in Directory.GetFiles("./TestFiles/Bitvavo/", "*.csv", SearchOption.AllDirectories))
			{
				// Act
				var canParse = await parser.CanParseActivities(file);

				// Assert
				canParse.Should().BeTrue($"File {file}  cannot be parsed");
			}
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleBuy_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Bitvavo/BuyOrders/single_buy.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateBuy(
						Currency.EUR,
						new DateTime(2023, 12, 13, 14, 39, 02, 473, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateCrypto("STORJ")],
						34.75825253M,
						0.71746M,
						"16eed6ae-65f9-4a9d-8f19-bd66a75fc745"),
					PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2023, 12, 13, 14, 39, 02, 473, DateTimeKind.Utc),
						0.0623441398262M,
						"16eed6ae-65f9-4a9d-8f19-bd66a75fc745"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleReceive_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Bitvavo/Receive/single_receive.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateReceive(
						new DateTime(2023, 10, 13, 22, 38, 36, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateCrypto("ATOM")],
						15.586311M,
						"af86c3d8-ff57-4866-b6ce-7a549db31eda"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleSend_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Bitvavo/Send/single_send.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateSend(
						new DateTime(2023, 09, 07, 21, 39, 25, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateCrypto("XLM")],
						87.339658M,
						"7d62b00a-60c2-42ac-b10e-4199e54cf4c8"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleDeposit_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Bitvavo/CashTransactions/single_deposit.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateCashDeposit(
						Currency.EUR,
						new DateTime(2023, 04, 21, 08, 48, 55, DateTimeKind.Utc),
						1M,
						"796a11aa-998f-4425-a503-07543300cda1"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleWithdrawal_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Bitvavo/CashTransactions/single_withdrawal.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateCashWithdrawal(
						Currency.EUR,
						new DateTime(2023, 10, 24, 21, 23, 37, DateTimeKind.Utc),
						101.88M,
						"1e651f3e-e5be-4a87-a8fa-00e6832bdbc7"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleSell_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Bitvavo/SellOrders/single_sell.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateSell(
						Currency.EUR,
						new DateTime(2023, 12, 13, 14, 45, 51, 803, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateCrypto("ADA")],
						45.802549M,
						0.56714M,
						"14ae873a-4fce-4a12-ba0f-387522c67d46"),
					PartialActivity.CreateFee(
						Currency.EUR,
						new DateTime(2023, 12, 13, 14, 45, 51, 803, DateTimeKind.Utc),
						0.04645763986M,
						"14ae873a-4fce-4a12-ba0f-387522c67d46"),
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleStakeReward_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Bitvavo/Specials/single_stakingreward.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateStakingReward(
						new DateTime(2023, 12, 11, 10, 32, 26, DateTimeKind.Utc),
						[PartialSymbolIdentifier.CreateCrypto("AXS")],
						0.00000272M,
						"15895215-8c11-4497-8151-eb5e5701180e")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleAffiliate_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Bitvavo/Specials/single_affiliate.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateGift(
						Currency.EUR,
						new DateTime(2023, 05, 15, 04, 00, 43, DateTimeKind.Utc),
						0.03M,
						"2f963a7d-891a-481d-84df-90ac7aea2f8d")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleRebate_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Bitvavo/Specials/single_rebate.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEquivalentTo(
				[
					PartialActivity.CreateGift(
						Currency.EUR,
						new DateTime(2023, 04, 25, 21, 54, 06, DateTimeKind.Utc),
						0.06M,
						"d9c2f368-ee69-4fe6-a448-dd363953bb2b")
				]);
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_SingleBuy_Pending_Converted()
		{
			// Arrange

			// Act
			await parser.ParseActivities("./TestFiles/Bitvavo/Invalid/single_buy_pending.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			holdingsAndAccountsCollection.PartialActivities.Should().BeEmpty();
		}

		[Fact]
		public async Task ConvertActivitiesForAccount_InvalidType_ThrowsException()
		{
			// Arrange

			// Act
			Func<Task> a = async () => await parser.ParseActivities("./TestFiles/Bitvavo/Invalid/invalid_type.csv", holdingsAndAccountsCollection, account.Name);

			// Assert
			await a.Should().ThrowAsync<NotSupportedException>();
		}
	}
}