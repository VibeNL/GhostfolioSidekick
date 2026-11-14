using System.Text.Json.Nodes;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Tools.ScraperUtilities.Mcp;
using Microsoft.Extensions.Logging.Abstractions;

namespace GhostfolioSidekick.Tools.ScraperUtilities.UnitTests
{
	public class McpScraperTests
	{
		private const string BuyTradeDetail = """
		{
			"id": "t1",
			"kind": "security_trade",
			"currency": "EUR",
			"status": "SETTLED",
			"lastEventAt": "2025-06-30T00:00:00.000Z",
			"security": { "isin": "IE00B4L5Y983", "name": "Vanguard FTSE All-World" },
			"history": [
				{ "state": "FILLED", "timestamp": "2025-06-30T10:00:00" },
				{ "state": "SETTLED", "timestamp": "2025-07-02T00:00:00" }
			],
			"securityTrade": {
				"side": "BUY",
				"numberOfShares": { "filled": "10" },
				"averagePrice": "100.5",
				"fee": "1.89"
			}
		}
		""";

		private const string SellTradeDetail = """
		{
			"id": "t3",
			"kind": "security_trade",
			"currency": "EUR",
			"status": "SETTLED",
			"lastEventAt": "2025-06-30T00:00:00.000Z",
			"security": { "isin": "IE00B4L5Y983", "name": "Vanguard FTSE All-World" },
			"history": [
				{ "state": "FILLED", "timestamp": "2025-06-30T10:00:00" }
			],
			"securityTrade": {
				"side": "SELL",
				"numberOfShares": { "filled": "-10" },
				"averagePrice": "200"
			}
		}
		""";

		private const string WithdrawalDetail = """
		{
			"id": "t2",
			"kind": "cash",
			"currency": "EUR",
			"status": "SETTLED",
			"lastEventAt": "2025-07-01T00:00:00.000Z",
			"history": [ { "state": "FILLED", "timestamp": "2025-07-01T09:30:00" } ],
			"cash": {
				"transactionType": "WITHDRAWAL",
				"amount": "-50",
				"description": "Withdrawal to bank account"
			}
		}
		""";

		private const string DepositDetail = """
		{
			"id": "t-deposit",
			"kind": "cash",
			"currency": "EUR",
			"status": "SETTLED",
			"lastEventAt": "2025-07-01T00:00:00.000Z",
			"history": [ { "state": "FILLED", "timestamp": "2025-07-01T09:30:00" } ],
			"cash": { "transactionType": "DEPOSIT", "amount": "250" }
		}
		""";

		private const string DividendDetail = """
		{
			"id": "t-dividend",
			"kind": "cash",
			"currency": "EUR",
			"status": "SETTLED",
			"lastEventAt": "2025-07-01T00:00:00.000Z",
			"history": [ { "state": "FILLED", "timestamp": "2025-07-01T09:30:00" } ],
			"cash": { "transactionType": "DISTRIBUTION", "amount": "12.34" }
		}
		""";

		private const string InterestDetail = """
		{
			"id": "t-interest",
			"kind": "cash",
			"currency": "EUR",
			"status": "SETTLED",
			"lastEventAt": "2025-07-01T00:00:00.000Z",
			"history": [ { "state": "FILLED", "timestamp": "2025-07-01T09:30:00" } ],
			"cash": { "transactionType": "INTEREST_PAYMENT", "amount": "0.42" }
		}
		""";

		private const string TransferOutDetail = """
		{
			"id": "t-transfer",
			"kind": "cash",
			"currency": "EUR",
			"status": "SETTLED",
			"lastEventAt": "2025-07-01T00:00:00.000Z",
			"history": [ { "state": "FILLED", "timestamp": "2025-07-01T09:30:00" } ],
			"cash": { "transactionType": "CASH_TRANSFER_OUT", "amount": "-75" }
		}
		""";

		private const string NonTradeSecurityDetail = """
		{
			"id": "t-nts",
			"kind": "non_trade_security",
			"currency": "EUR",
			"status": "SETTLED",
			"lastEventAt": "2025-12-06T00:00:00.000Z",
			"security": { "isin": "US72201Y1010", "name": "Pimco Dynamic Income Fund" },
			"nonTradeSecurity": { "transactionType": "TRANSFER_IN", "quantity": "116", "averagePrice": "15.5", "totalAmount": "1798" }
		}
		""";

		private const string UnknownKindDetail = """
		{
			"id": "t-eltif",
			"kind": "eltif",
			"currency": "EUR",
			"status": "SETTLED",
			"lastEventAt": "2025-07-01T00:00:00.000Z"
		}
		""";

		private const string OvernightTransactions = """
		{
			"transactions": [
				{ "id": "ov1", "status": "SETTLED", "cashTransactionType": "INTEREST_PAYMENT", "amount": "0.42", "currency": "EUR", "lastEventAt": "2025-07-01T00:00:00.000Z" },
				{ "id": "ov2", "status": "SETTLED", "cashTransactionType": "WITHDRAWAL", "amount": "-100", "currency": "EUR", "lastEventAt": "2025-07-02T00:00:00.000Z" }
			]
		}
		""";

		[Fact]
		public async Task ScrapeTransactionsAsync_WithdrawalWithNegativeAmount_ProducesPositiveCashWithdrawal()
		{
			var client = CreateClient(["t2"], new() { ["t2"] = WithdrawalDetail });
			var scraper = new McpScraper(client, NullLogger.Instance);

			var result = await scraper.ScrapeTransactionsAsync(CancellationToken.None);

			var activityWithSymbol = Assert.Single(Assert.Single(result).Value);
			var withdrawal = Assert.IsType<CashWithdrawalActivity>(activityWithSymbol.Activity);
			Assert.Equal(new Money(Currency.EUR, 50m), withdrawal.Amount);
			Assert.Equal("t2", withdrawal.TransactionId);
		}

		[Fact]
		public async Task ScrapeTransactionsAsync_BuyTrade_MapsQuantityPriceFeesAndDate()
		{
			var client = CreateClient(["t1"], new() { ["t1"] = BuyTradeDetail });
			var scraper = new McpScraper(client, NullLogger.Instance);

			var result = await scraper.ScrapeTransactionsAsync(CancellationToken.None);

			var activityWithSymbol = Assert.Single(Assert.Single(result).Value);
			Assert.Equal("IE00B4L5Y983", activityWithSymbol.Symbol);
			var buy = Assert.IsType<BuyActivity>(activityWithSymbol.Activity);
			Assert.Equal(10m, buy.Quantity);
			Assert.Equal(new Money(Currency.EUR, 100.5m), buy.UnitPrice);
			Assert.Equal(new Money(Currency.EUR, 1.89m), Assert.Single(buy.Fees));
			Assert.Empty(buy.Taxes);
			// Naive timestamps are treated as UTC; compare instants so the assertion is machine-timezone independent.
			Assert.Equal(new DateTime(2025, 6, 30, 10, 0, 0, DateTimeKind.Utc), buy.Date.ToUniversalTime());
			Assert.Equal("t1", buy.TransactionId);
		}

		[Fact]
		public async Task ScrapeTransactionsAsync_SellTradeWithNegativeFilledQuantity_MapsPositiveQuantity()
		{
			var client = CreateClient(["t3"], new() { ["t3"] = SellTradeDetail });
			var scraper = new McpScraper(client, NullLogger.Instance);

			var result = await scraper.ScrapeTransactionsAsync(CancellationToken.None);

			var sell = Assert.IsType<SellActivity>(Assert.Single(Assert.Single(result).Value).Activity);
			Assert.Equal(10m, sell.Quantity);
			Assert.Equal(new Money(Currency.EUR, 200m), sell.UnitPrice);
		}

		[Fact]
		public async Task ScrapeTransactionsAsync_CashTypes_MapToCorrectActivities()
		{
			var client = CreateClient(["t-deposit", "t-dividend", "t-interest"], new()
			{
				["t-deposit"] = DepositDetail,
				["t-dividend"] = DividendDetail,
				["t-interest"] = InterestDetail
			});
			var scraper = new McpScraper(client, NullLogger.Instance);

			var result = await scraper.ScrapeTransactionsAsync(CancellationToken.None);
			var activities = Assert.Single(result).Value.ToList();

			var deposit = Assert.IsType<CashDepositActivity>(activities.First(a => a.Activity is CashDepositActivity).Activity);
			Assert.Equal(new Money(Currency.EUR, 250m), deposit.Amount);

			var dividend = Assert.IsType<DividendActivity>(activities.First(a => a.Activity is DividendActivity).Activity);
			Assert.Equal(new Money(Currency.EUR, 12.34m), dividend.Amount);

			var interest = Assert.IsType<InterestActivity>(activities.First(a => a.Activity is InterestActivity).Activity);
			Assert.Equal(new Money(Currency.EUR, 0.42m), interest.Amount);
		}

		[Fact]
		public async Task ScrapeTransactionsAsync_CashTransferOut_IsSkipped()
		{
			var client = CreateClient(["t-transfer"], new() { ["t-transfer"] = TransferOutDetail });
			var scraper = new McpScraper(client, NullLogger.Instance);

			var result = await scraper.ScrapeTransactionsAsync(CancellationToken.None);

			Assert.Empty(Assert.Single(result).Value);
		}

		[Fact]
		public async Task ScrapeTransactionsAsync_NonTradeSecurity_IsSkippedWithoutError()
		{
			var client = CreateClient(["t-nts"], new() { ["t-nts"] = NonTradeSecurityDetail });
			var scraper = new McpScraper(client, NullLogger.Instance);

			var result = await scraper.ScrapeTransactionsAsync(CancellationToken.None);

			Assert.Empty(Assert.Single(result).Value);
		}

		[Fact]
		public async Task ScrapeTransactionsAsync_UnknownKind_IsSkippedWithoutAborting()
		{
			var client = CreateClient(["t-eltif"], new() { ["t-eltif"] = UnknownKindDetail });
			var scraper = new McpScraper(client, NullLogger.Instance);

			var result = await scraper.ScrapeTransactionsAsync(CancellationToken.None);

			Assert.Empty(Assert.Single(result).Value);
		}

		[Fact]
		public async Task ScrapePortfolioAsync_FollowsPaginationCursor()
		{
			var details = new Dictionary<string, JsonObject>
			{
				["t1"] = ParseObject(BuyTradeDetail),
				["t2"] = ParseObject(WithdrawalDetail)
			};

			var client = new FakeMcpClient((toolName, arguments) => toolName switch
			{
				"list_accessible_portfolios" => Portfolios("P1"),
				"list_portfolio_transactions" when arguments?["cursor"] is null => TransactionPageWithCursor(ListEntry("t1"), "c2"),
				"list_portfolio_transactions" => TransactionPage([ListEntry("t2")]),
				"get_transaction_details" => Detail(details[arguments?["transactionId"]?.ToString()!]),
				"list_overnight_transactions" => throw new McpException("No active overnight savings account."),
				_ => throw new InvalidOperationException($"Unexpected MCP tool '{toolName}'.")
			});
			var scraper = new McpScraper(client, NullLogger.Instance);

			var result = await scraper.ScrapeTransactionsAsync(CancellationToken.None);

			var activities = Assert.Single(result).Value.ToList();
			Assert.Equal(2, activities.Count);
			Assert.Contains(activities, a => a.Activity is BuyActivity);
			Assert.Contains(activities, a => a.Activity is CashWithdrawalActivity);
		}

		[Fact]
		public async Task ScrapeTransactionsAsync_OvernightAccount_MapsInterestAndPositiveWithdrawal()
		{
			var client = CreateClient([], new Dictionary<string, string>(), OvernightTransactions);
			var scraper = new McpScraper(client, NullLogger.Instance);

			var result = await scraper.ScrapeTransactionsAsync(CancellationToken.None);

			Assert.Equal(2, result.Count);
			var overnight = result[2].ToList();
			var interest = Assert.IsType<InterestActivity>(overnight.First(a => a.Activity is InterestActivity).Activity);
			Assert.Equal(new Money(Currency.EUR, 0.42m), interest.Amount);
			var withdrawal = Assert.IsType<CashWithdrawalActivity>(overnight.First(a => a.Activity is CashWithdrawalActivity).Activity);
			Assert.Equal(new Money(Currency.EUR, 100m), withdrawal.Amount);
		}

		[Fact]
		public async Task ScrapeTransactionsAsync_NoOvernightAccount_StillReturnsBrokerPortfolios()
		{
			var client = CreateClient(["t2"], new() { ["t2"] = WithdrawalDetail });
			var scraper = new McpScraper(client, NullLogger.Instance);

			var result = await scraper.ScrapeTransactionsAsync(CancellationToken.None);

			Assert.Single(result);
			Assert.IsType<CashWithdrawalActivity>(Assert.Single(Assert.Single(result).Value).Activity);
		}

		[Fact]
		public async Task ScrapeTransactionsAsync_NoPortfoliosAndNoOvernight_Throws()
		{
			var client = new FakeMcpClient((toolName, _) => toolName switch
			{
				"list_accessible_portfolios" => Portfolios(),
				"list_overnight_transactions" => throw new McpException("No active overnight savings account."),
				_ => throw new InvalidOperationException($"Unexpected MCP tool '{toolName}'.")
			});
			var scraper = new McpScraper(client, NullLogger.Instance);

			await Assert.ThrowsAsync<McpException>(() => scraper.ScrapeTransactionsAsync(CancellationToken.None));
		}

		private static FakeMcpClient CreateClient(
			string[] transactionIds,
			Dictionary<string, string> detailJsonByTransactionId,
			string? overnightTransactions = null)
		{
			var details = detailJsonByTransactionId.ToDictionary(kv => kv.Key, kv => ParseObject(kv.Value));

			return new FakeMcpClient((toolName, arguments) => toolName switch
			{
				"list_accessible_portfolios" => Portfolios("P1"),
				"list_portfolio_transactions" => TransactionPage(transactionIds.Select(id => ListEntry(id)).ToArray()),
				"get_transaction_details" => Detail(details[arguments?["transactionId"]?.ToString()!]),
				"list_overnight_transactions" => overnightTransactions is null
					? throw new McpException("No active overnight savings account.")
					: ParseObject(overnightTransactions),
				_ => throw new InvalidOperationException($"Unexpected MCP tool '{toolName}'.")
			});
		}

		private static JsonObject Portfolios(params string[] portfolioIds) => new()
		{
			["portfolios"] = new JsonArray(portfolioIds.Select(id => (JsonNode)new JsonObject { ["portfolioId"] = id }).ToArray())
		};

		private static JsonObject TransactionPage(params JsonObject[] transactions)
		{
			var page = new JsonObject();
			return new()
			{
				["transactions"] = new JsonArray(transactions),
				["page"] = page
			};
		}

		private static JsonObject TransactionPageWithCursor(JsonObject transaction, string nextCursor)
		{
			var page = new JsonObject { ["nextCursor"] = nextCursor };
			return new()
			{
				["transactions"] = new JsonArray(new[] { transaction }),
				["page"] = page
			};
		}

		private static JsonObject ListEntry(string id, string status = "SETTLED") => new() { ["id"] = id, ["status"] = status };

		private static JsonObject Detail(JsonObject transaction) => new() { ["transaction"] = transaction };

		private static JsonObject ParseObject(string json) => (JsonNode.Parse(json) as JsonObject)!;
	}
}
