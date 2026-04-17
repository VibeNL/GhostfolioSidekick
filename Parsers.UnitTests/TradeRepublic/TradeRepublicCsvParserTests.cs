using AutoFixture;
using AwesomeAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.TradeRepublic;

namespace GhostfolioSidekick.Parsers.UnitTests.TradeRepublic
{
    public class TradeRepublicCsvParserTests
    {
        private readonly TradeRepublicCsvParser parser;
        private readonly Account account;
        private readonly TestActivityManager activityManager;

        public TradeRepublicCsvParserTests()
        {
            parser = new TradeRepublicCsvParser(DummyCurrencyMapper.Instance);

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
            foreach (var file in Directory.GetFiles("./TestFiles/TradeRepublic/CSV/", "*.csv", SearchOption.AllDirectories))
            {
                var canParse = await parser.CanParse(file);
                canParse.Should().BeTrue($"File {file} cannot be parsed");
            }
        }

        [Fact]
        public async Task ConvertActivitiesForAccount_SingleBuyStock_Converted()
        {
            await parser.ParseActivities("./TestFiles/TradeRepublic/CSV/single_buy_stock.csv", activityManager, account.Name);

            activityManager.PartialActivities.Should().BeEquivalentTo(
            [
                PartialActivity.CreateBuy(
                    Currency.EUR,
                    new DateTime(2024, 01, 16, 20, 47, 27, 452, DateTimeKind.Utc),
                    [
                        PartialSymbolIdentifier.CreateStockBondAndETF(IdentifierType.ISIN, "US2546871060", Currency.EUR),
                        PartialSymbolIdentifier.CreateStockBondAndETF(IdentifierType.Name, "Walt Disney", Currency.EUR),
                    ],
                    0.0603570000m,
                    new Money(Currency.EUR, 82.840000m),
                    new Money(Currency.EUR, 5.00m),
                    "770b8077-3e7e-4b36-a9b3-ea3ff615b07b")
            ]);
        }

        [Fact]
        public async Task ConvertActivitiesForAccount_SingleBuyWithFee_Converted()
        {
            await parser.ParseActivities("./TestFiles/TradeRepublic/CSV/single_buy_with_fee.csv", activityManager, account.Name);

            activityManager.PartialActivities.Should().BeEquivalentTo(
            [
                PartialActivity.CreateBuy(
                    Currency.EUR,
                    new DateTime(2024, 05, 02, 15, 21, 43, 161, DateTimeKind.Utc),
                    [
                        PartialSymbolIdentifier.CreateStockBondAndETF(IdentifierType.ISIN, "IE0032895942", Currency.EUR),
                        PartialSymbolIdentifier.CreateStockBondAndETF(IdentifierType.Name, "Corp Bond USD (Dist)", Currency.EUR),
                    ],
                    0.5372640000m,
                    new Money(Currency.EUR, 93.064000m),
                    new Money(Currency.EUR, 50.00m),
                    "ec3ef519-39e2-4fe1-a810-2ee8425bf877"),
                PartialActivity.CreateFee(
                    Currency.EUR,
                    new DateTime(2024, 05, 02, 15, 21, 43, 161, DateTimeKind.Utc),
                    1.00m,
                    new Money(Currency.EUR, 1.00m),
                    "ec3ef519-39e2-4fe1-a810-2ee8425bf877_FEE")
            ]);
        }

        [Fact]
        public async Task ConvertActivitiesForAccount_SingleDividend_Converted()
        {
            await parser.ParseActivities("./TestFiles/TradeRepublic/CSV/single_dividend.csv", activityManager, account.Name);

            activityManager.PartialActivities.Should().BeEquivalentTo(
            [
                PartialActivity.CreateDividend(
                    Currency.EUR,
                    new DateTime(2024, 01, 10, 0, 55, 48, 147, DateTimeKind.Utc).AddMicroseconds(672),
                    [
                        PartialSymbolIdentifier.CreateStockBondAndETF(IdentifierType.ISIN, "US2546871060", Currency.EUR),
                        PartialSymbolIdentifier.CreateStockBondAndETF(IdentifierType.Name, "Walt Disney", Currency.EUR),
                    ],
                    0.090000m,
                    new Money(Currency.EUR, 0.090000m),
                    "895b5983-4d52-4614-b521-a20eb7b7082c"),
                PartialActivity.CreateTax(
                    Currency.EUR,
                    new DateTime(2024, 01, 10, 0, 55, 48, 147, DateTimeKind.Utc).AddMicroseconds(672),
                    0.02m,
                    new Money(Currency.EUR, 0.02m),
                    "895b5983-4d52-4614-b521-a20eb7b7082c_TAX")
            ]);
        }

        [Fact]
        public async Task ConvertActivitiesForAccount_SingleInterest_Converted()
        {
            await parser.ParseActivities("./TestFiles/TradeRepublic/CSV/single_interest.csv", activityManager, account.Name);

            activityManager.PartialActivities.Should().BeEquivalentTo(
            [
                PartialActivity.CreateInterest(
                    Currency.EUR,
                    new DateTime(2024, 02, 01, 12, 46, 23, 598, DateTimeKind.Utc).AddMicroseconds(279),
                    30.730000m,
                    "Interest payment Booking",
                    new Money(Currency.EUR, 30.730000m),
                    "86f5a92e-edf0-47d6-9e17-c6fdf3fc150f")
            ]);
        }

        [Fact]
        public async Task ConvertActivitiesForAccount_SingleDepositInpayment_Converted()
        {
            await parser.ParseActivities("./TestFiles/TradeRepublic/CSV/single_deposit_inpayment.csv", activityManager, account.Name);

            activityManager.PartialActivities.Should().BeEquivalentTo(
            [
                PartialActivity.CreateCashDeposit(
                    Currency.EUR,
                    new DateTime(2023, 10, 06, 11, 32, 20, 493, DateTimeKind.Utc).AddMicroseconds(611),
                    250.000000m,
                    new Money(Currency.EUR, 250.000000m),
                    "7d1109df-ed7b-40aa-b3f9-389e8d4c64cc")
            ]);
        }

        [Fact]
        public async Task ConvertActivitiesForAccount_SingleDepositInbound_Converted()
        {
            await parser.ParseActivities("./TestFiles/TradeRepublic/CSV/single_deposit_inbound.csv", activityManager, account.Name);

            activityManager.PartialActivities.Should().BeEquivalentTo(
            [
                PartialActivity.CreateCashDeposit(
                    Currency.EUR,
                    new DateTime(2023, 10, 16, 11, 24, 57, 175, DateTimeKind.Utc).AddMicroseconds(23),
                    1000.000000m,
                    new Money(Currency.EUR, 1000.000000m),
                    "31ef2829-a92c-4fe0-a40b-768c112a6b5b")
            ]);
        }

        [Fact]
        public async Task ConvertActivitiesForAccount_SingleWithdrawal_Converted()
        {
            await parser.ParseActivities("./TestFiles/TradeRepublic/CSV/single_withdrawal.csv", activityManager, account.Name);

            activityManager.PartialActivities.Should().BeEquivalentTo(
            [
                PartialActivity.CreateCashWithdrawal(
                    Currency.EUR,
                    new DateTime(2023, 12, 30, 19, 42, 59, 758, DateTimeKind.Utc).AddMicroseconds(754),
                    1000.000000m,
                    new Money(Currency.EUR, 1000.000000m),
                    "7277cc7d-eb4f-4aaa-b6b6-5c45d316d52d")
            ]);
        }

        [Fact]
        public async Task ConvertActivitiesForAccount_SingleBondRedemption_Converted()
        {
            await parser.ParseActivities("./TestFiles/TradeRepublic/CSV/single_bond_redemption.csv", activityManager, account.Name);

            activityManager.PartialActivities.Should().BeEquivalentTo(
            [
                PartialActivity.CreateSell(
                    Currency.EUR,
                    new DateTime(2024, 02, 14, 23, 19, 02, 613, DateTimeKind.Utc),
                    [
                        PartialSymbolIdentifier.CreateStockBondAndETF(IdentifierType.ISIN, "XS0000000001", Currency.EUR),
                        PartialSymbolIdentifier.CreateStockBondAndETF(IdentifierType.Name, "Test Bond Feb. 2024", Currency.EUR),
                    ],
                    99.4700000000m,
                    new Money(Currency.EUR, 1.000000m),
                    new Money(Currency.EUR, 99.4700000000m),
                    "b942010b-8906-44f8-8889-1ff3da926bb0")
            ]);
        }

        [Fact]
        public async Task ConvertActivitiesForAccount_SingleSaveback_Converted()
        {
            await parser.ParseActivities("./TestFiles/TradeRepublic/CSV/single_saveback.csv", activityManager, account.Name);

            activityManager.PartialActivities.Should().BeEquivalentTo(
            [
                PartialActivity.CreateCashDeposit(
                    Currency.EUR,
                    new DateTime(2024, 05, 02, 14, 12, 34, 276, DateTimeKind.Utc).AddMicroseconds(480),
                    4.310000m,
                    new Money(Currency.EUR, 4.310000m),
                    "d802544e-9505-4fa9-b95e-32d91d7d91c7")
            ]);
        }

        [Fact]
        public async Task ConvertActivitiesForAccount_SingleCardTransaction_Converted()
        {
            await parser.ParseActivities("./TestFiles/TradeRepublic/CSV/single_card_transaction.csv", activityManager, account.Name);

            activityManager.PartialActivities.Should().BeEquivalentTo(
            [
                PartialActivity.CreateCashWithdrawal(
                    Currency.EUR,
                    new DateTime(2024, 04, 21, 07, 21, 17, 574, DateTimeKind.Utc).AddMicroseconds(610),
                    27.270000m,
                    new Money(Currency.EUR, 27.270000m),
                    "9e3705b8-65ea-410f-a92c-f39abb3706ac")
            ]);
        }

        [Fact]
        public async Task ConvertActivitiesForAccount_SingleCardTransactionInternational_Converted()
        {
            await parser.ParseActivities("./TestFiles/TradeRepublic/CSV/single_card_transaction_international.csv", activityManager, account.Name);

            activityManager.PartialActivities.Should().BeEquivalentTo(
            [
                PartialActivity.CreateCashWithdrawal(
                    Currency.EUR,
                    new DateTime(2024, 08, 10, 10, 38, 47, 492, DateTimeKind.Utc).AddMicroseconds(490),
                    27.800000m,
                    new Money(Currency.EUR, 27.800000m),
                    "a0f25981-6033-4e52-8826-7597b7d9aafe")
            ]);
        }

        [Fact]
        public async Task ConvertActivitiesForAccount_SingleTransferInstantInbound_Converted()
        {
            await parser.ParseActivities("./TestFiles/TradeRepublic/CSV/single_transfer_instant_inbound.csv", activityManager, account.Name);

            activityManager.PartialActivities.Should().BeEquivalentTo(
            [
                PartialActivity.CreateCashDeposit(
                    Currency.EUR,
                    new DateTime(2025, 07, 31, 04, 36, 25, 416, DateTimeKind.Utc).AddMicroseconds(954),
                    5.000000m,
                    new Money(Currency.EUR, 5.000000m),
                    "b5960a9c-4a8d-4491-9586-b164479dcc4d")
            ]);
        }

        [Fact]
        public async Task ConvertActivitiesForAccount_SingleTransferInstantOutbound_Converted()
        {
            await parser.ParseActivities("./TestFiles/TradeRepublic/CSV/single_transfer_instant_outbound.csv", activityManager, account.Name);

            activityManager.PartialActivities.Should().BeEquivalentTo(
            [
                PartialActivity.CreateCashWithdrawal(
                    Currency.EUR,
                    new DateTime(2025, 12, 02, 06, 05, 56, 100, DateTimeKind.Utc).AddMicroseconds(359),
                    10000.000000m,
                    new Money(Currency.EUR, 10000.000000m),
                    "019addaa-e604-709b-996c-dcfab3e22262")
            ]);
        }

        [Fact]
        public async Task ConvertActivitiesForAccount_SingleBonus_Converted()
        {
            await parser.ParseActivities("./TestFiles/TradeRepublic/CSV/single_bonus.csv", activityManager, account.Name);

            activityManager.PartialActivities.Should().BeEquivalentTo(
            [
                PartialActivity.CreateCashDeposit(
                    Currency.EUR,
                    new DateTime(2025, 10, 02, 10, 13, 50, 633, DateTimeKind.Utc).AddMicroseconds(670),
                    0.500000m,
                    new Money(Currency.EUR, 0.500000m),
                    "84ef1ad6-e42e-4af9-ad76-57d20005b29e")
            ]);
        }

        [Fact]
        public async Task ConvertActivitiesForAccount_SinglePrivateMarketBuy_Ignored()
        {
            await parser.ParseActivities("./TestFiles/TradeRepublic/CSV/single_private_market_buy.csv", activityManager, account.Name);

            activityManager.PartialActivities.Should().BeEmpty();
        }

        [Fact]
        public async Task ConvertActivitiesForAccount_SingleMigration_Ignored()
        {
            await parser.ParseActivities("./TestFiles/TradeRepublic/CSV/single_migration.csv", activityManager, account.Name);

            activityManager.PartialActivities.Should().BeEmpty();
        }
    }
}
