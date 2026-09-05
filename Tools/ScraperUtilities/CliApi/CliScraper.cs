using System.Globalization;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Tools.ScraperUtilities;

namespace GhostfolioSidekick.Tools.ScraperUtilities.CliApi
{
	/// <summary>
	/// Scrapes Scalable Capital transactions via the official CLI API (same GraphQL endpoint as scalable-cli).
	/// Unlike the MCP server, this path also returns overnight savings-account interest.
	/// Produces the same output contract as the other scrapers: account index to activities.
	/// </summary>
	public class CliScraper
	{
		private const int PageSize = 100;

		private static readonly string ResolveBrokerIdsQuery = @"
query ResolveBrokerIds($id: ID!) {
  account(id: $id) {
    id
    brokerPortfolios {
      id
    }
  }
}";

		private static readonly string BrokerTransactionsQuery = @"
query BrokerTransactions($accountId: ID!, $portfolioId: ID!, $input: BrokerTransactionInput!) {
  account(id: $accountId) {
    brokerPortfolio(id: $portfolioId) {
      moreTransactions(input: $input) {
        cursor
        total
        transactions {
          __typename
          id
          currency
          type
          status
          isCancellation
          lastEventDateTime
          description
          custodian
          documents {
            id
            url
            label
          }
          ... on BrokerSecurityTransactionSummary {
            isin
            securityTransactionType
            quantity
            amount
            side
            limitPrice
            stopPrice
          }
          ... on BrokerCashTransactionSummary {
            relatedIsin
            cashTransactionType
            amount
          }
          ... on BrokerNonTradeSecurityTransactionSummary {
            isin
            nonTradeSecurityTransactionType
            quantity
            amount
          }
          ... on BrokerEltifTransactionSummary {
            isin
            securityTransactionType
            eltifQuantity
            amount
            side
          }
        }
      }
    }
  }
}";

		private static readonly string BrokerTransactionDetailsQuery = @"
query BrokerTransactionDetails($accountId: ID!, $portfolioId: ID!, $transactionId: ID!) {
  account(id: $accountId) {
    brokerPortfolio(id: $portfolioId) {
      transactionDetails(id: $transactionId) {
        __typename
        id
        currency
        type
        documents {
          id
          url
          label
        }
        lastEventDateTime
        isPending
        isCancellation
        security {
          isin
          name
          type
        }
        transactionReference
        ... on BrokerSecurityTransaction {
          side
          status
          numberOfShares {
            filled
            total
          }
          averagePrice
          totalAmount
          finalisationReason
          limitPrice
          stopPrice
          validUntil
          isCancellationRequested
          tradeTransactionAmounts {
            marketValuation
            taxAmount
            transactionFee
            venueFee
            cryptoSpreadFee
          }
          tradingVenue
          fee
          transactionalFee
          taxes
          aggregatedTransactionTaxes {
            totalTax
            capitalGainsTax
            churchTax
            solidarityTax
            sourceTax
            financialTransactionTax
          }
          securityTransactionHistory: transactionHistory {
            state
            time {
              time
              epochSecond
              epochMillisecond
            }
            numberOfShares {
              filled
              total
            }
            executionPrice
          }
          orderKind
          linkedTransactions {
            id
          }
          trailingStopInfo {
            trailType
            trailOffset
            latestStopPriceTimestamp {
              time
              epochSecond
              epochMillisecond
            }
          }
        }
        ... on BrokerCashTransaction {
          cashTransactionType
          amount
          description
          cashTransactionHistory: transactionHistory {
            state
            time {
              time
              epochSecond
              epochMillisecond
            }
          }
          sddiDetails {
            fee
            grossAmount
          }
          taxDetails {
            grossAmount
            taxAmount
          }
          linkedTransactions {
            id
          }
        }
        ... on BrokerNonTradeSecurityTransaction {
          isin
          nonTradeSecurityTransactionType
          quantity
          nonTradeAveragePrice: averagePrice
          nonTradeSecurityAmount: totalAmount
          description
          nonTradeSecurityTransactionHistory: transactionHistory {
            state
            time {
              time
              epochSecond
              epochMillisecond
            }
          }
          linkedTransactions {
            id
          }
        }
        ... on BrokerEltifTransaction {
          status
          side
          orderKind
          amount
          finalisationReason
          eltifQuantity
          executionPrice
          executionDate
          earliestSellDate
          marketValuation
          cancelableDetails {
            daysLeft
            isCancelable
          }
          isMultipleOrdersCancellation
          isInitialInvestment
          tradingVenue
          eltifTransactionHistory: transactionHistory {
            state
            amount
            eltifQuantity
            executionPrice
            time {
              time
              epochSecond
              epochMillisecond
            }
          }
          linkedTransactions {
            id
          }
        }
      }
    }
  }
}";

		private static readonly string DiscoverOvernightAccountsQuery = @"
query DiscoverOvernightAccounts($accountId: ID!) {
  account(id: $accountId) {
    savingsAccounts {
      __typename
      id
      personalizations {
        name
      }
      state
    }
  }
}";

		private static readonly string OvernightTransactionsQuery = @"
query OvernightTransactions(
  $accountId: ID!
  $savingsAccountId: ID!
  $input: SavingsAccountCashTransactionInput!
) {
  account(id: $accountId) {
    savingsAccount(id: $savingsAccountId) {
      id
      moreTransactions(input: $input) {
        cursor
        total
        transactions {
          id
          currency
          type
          status
          isCancellation
          lastEventDateTime
          description
          cashTransactionType
          amount
          custodian
          relatedIsin
          documents {
            id
            label
            url
          }
        }
      }
    }
  }
}";

		private readonly CliGraphqlClient _client;
		private readonly CliTokenProvider _tokenProvider;
		private readonly ILogger _logger;

		public CliScraper(CliGraphqlClient client, CliTokenProvider tokenProvider, ILogger logger)
		{
			_client = client;
			_tokenProvider = tokenProvider;
			_logger = logger;
		}

		public async Task<Dictionary<int, IEnumerable<ActivityWithSymbol>>> ScrapeTransactionsAsync(CancellationToken cancellationToken)
		{
			var session = await _tokenProvider.GetSessionAsync(cancellationToken);
			var portfolioIds = await ResolveBrokerPortfolioIdsAsync(session.PersonId, cancellationToken);

			var result = new Dictionary<int, IEnumerable<ActivityWithSymbol>>();
			var index = 0;
			foreach (var portfolioId in portfolioIds)
			{
				index++;
				_logger.LogInformation("Scraping Scalable Capital CLI transactions for portfolio {PortfolioId}", portfolioId);
				result[index] = await ScrapeBrokerPortfolioAsync(session.PersonId, portfolioId, cancellationToken);
			}

			var savingsAccounts = await DiscoverOvernightSavingsAccountsAsync(session.PersonId, cancellationToken);
			foreach (var (savingsAccountId, name) in savingsAccounts)
			{
				index++;
				_logger.LogInformation("Scraping Scalable Capital overnight transactions for savings account {Name} ({SavingsAccountId})", name, savingsAccountId);
				result[index] = await ScrapeOvernightAccountAsync(session.PersonId, savingsAccountId, cancellationToken);
			}

			if (result.Count == 0)
			{
				throw new CliApiException("No accessible Scalable Capital portfolios or overnight accounts were returned by the CLI API.");
			}

			return result;
		}

		private async Task<List<string>> ResolveBrokerPortfolioIdsAsync(string personId, CancellationToken cancellationToken)
		{
			var data = await _client.QueryAsync(ResolveBrokerIdsQuery, new JsonObject { ["id"] = personId }, "ResolveBrokerIds", cancellationToken);
			var portfolios = data["account"]?["brokerPortfolios"]?.AsArray();

			return (portfolios ?? Enumerable.Empty<JsonNode?>())
				.Select(p => p?["id"]?.ToString())
				.Where(id => !string.IsNullOrWhiteSpace(id))
				.Select(id => id!)
				.ToList();
		}

		private async Task<List<(string Id, string Name)>> DiscoverOvernightSavingsAccountsAsync(string personId, CancellationToken cancellationToken)
		{
			var data = await _client.QueryAsync(DiscoverOvernightAccountsQuery, new JsonObject { ["accountId"] = personId }, "DiscoverOvernightAccounts", cancellationToken);
			var accounts = data["account"]?["savingsAccounts"]?.AsArray();

			var result = new List<(string Id, string Name)>();
			foreach (var account in accounts ?? Enumerable.Empty<JsonNode?>())
			{
				if (account is not JsonObject item)
				{
					continue;
				}

				if (item["state"]?.ToString() != "ACTIVE")
				{
					continue;
				}

				var id = item["id"]?.ToString();
				if (string.IsNullOrWhiteSpace(id))
				{
					continue;
				}

				result.Add((id!, item["personalizations"]?["name"]?.ToString() ?? string.Empty));
			}

			return result;
		}

		private async Task<List<ActivityWithSymbol>> ScrapeBrokerPortfolioAsync(string personId, string portfolioId, CancellationToken cancellationToken)
		{
			var transactions = new List<JsonObject>();
			string? cursor = null;

			do
			{
				var input = new JsonObject { ["pageSize"] = PageSize };
				if (cursor != null)
				{
					input["cursor"] = cursor;
				}

				var variables = new JsonObject
				{
					["accountId"] = personId,
					["portfolioId"] = portfolioId,
					["input"] = input
				};

				var data = await _client.QueryAsync(BrokerTransactionsQuery, variables, "BrokerTransactions", cancellationToken);
				var moreTransactions = data["account"]?["brokerPortfolio"]?["moreTransactions"];
				foreach (var transaction in moreTransactions?["transactions"]?.AsArray() ?? Enumerable.Empty<JsonNode?>())
				{
					if (transaction is JsonObject item)
					{
						transactions.Add(item);
					}
				}

				cursor = moreTransactions?["cursor"]?.ToString();
			} while (!string.IsNullOrWhiteSpace(cursor));

			var activities = new List<ActivityWithSymbol>();
			foreach (var transaction in transactions)
			{
				try
				{
					var activity = await MapBrokerTransactionAsync(personId, portfolioId, transaction, cancellationToken);
					if (activity != null)
					{
						activities.Add(activity);
					}
				}
				catch (CliApiException ex)
				{
					_logger.LogWarning(ex, "Skipping CLI transaction {TransactionId}: {Message}", transaction["id"]?.ToString(), ex.Message);
				}
			}

			return activities;
		}

		private async Task<ActivityWithSymbol?> MapBrokerTransactionAsync(string personId, string portfolioId, JsonObject entry, CancellationToken cancellationToken)
		{
			var id = entry["id"]?.ToString();
			if (string.IsNullOrWhiteSpace(id))
			{
				return null;
			}

			var status = entry["status"]?.ToString() ?? string.Empty;
			if (status is not ("SETTLED" or "FILLED"))
			{
				_logger.LogDebug("Skipping CLI transaction {TransactionId} with status {Status}", id, status);
				return null;
			}

			var variables = new JsonObject
			{
				["accountId"] = personId,
				["portfolioId"] = portfolioId,
				["transactionId"] = id
			};

			var data = await _client.QueryAsync(BrokerTransactionDetailsQuery, variables, "BrokerTransactionDetails", cancellationToken);
			if (data["account"]?["brokerPortfolio"]?["transactionDetails"] is not JsonObject detail)
			{
				throw new CliApiException($"Transaction {id} has no detail payload.");
			}

			var typename = detail["__typename"]?.ToString() ?? "unknown";
			return typename switch
			{
				"BrokerSecurityTransaction" => MapSecurityTrade(detail),
				"BrokerCashTransaction" => MapCashTransaction(id, entry, detail),
				_ => throw new CliApiException($"Unknown transaction type '{typename}'.")
			};
		}

		private ActivityWithSymbol MapSecurityTrade(JsonObject detail)
		{
			var security = detail["security"];
			var isin = security?["isin"]?.ToString();
			if (string.IsNullOrWhiteSpace(isin))
			{
				throw new CliApiException("Security transaction has no ISIN.");
			}

			var side = detail["side"]?.ToString() ?? string.Empty;
			var quantity = ParseDecimal(detail["numberOfShares"]?["filled"]);
			var unitPrice = ParseMoney(detail["averagePrice"], detail);
			if (quantity is null || quantity <= 0m || unitPrice is null)
			{
				throw new CliApiException($"Security transaction {detail["id"]} has no filled quantity or price.");
			}

			var date = GetFilledDate(detail, "securityTransactionHistory");
			var currency = detail["currency"]?.ToString() ?? "EUR";
			var fees = CollectAmounts(currency, detail["fee"], detail["transactionalFee"], detail["tradeTransactionAmounts"]?["transactionFee"], detail["tradeTransactionAmounts"]?["venueFee"]);
			var taxes = CollectAmounts(currency, detail["taxes"], detail["tradeTransactionAmounts"]?["taxAmount"]);

			Activity activity = side switch
			{
				"BUY" => new BuyActivity { Quantity = quantity.Value, UnitPrice = unitPrice, Fees = fees, Taxes = taxes, Date = date, TransactionId = detail["id"]?.ToString() ?? string.Empty },
				"SELL" => new SellActivity { Quantity = quantity.Value, UnitPrice = unitPrice, Fees = fees, Taxes = taxes, Date = date, TransactionId = detail["id"]?.ToString() ?? string.Empty },
				_ => throw new CliApiException($"Unknown trade side '{side}'.")
			};

			return new ActivityWithSymbol
			{
				Activity = activity,
				Symbol = isin,
				SymbolName = security?["name"]?.ToString(),
				ISIN = isin
			};
		}

		private ActivityWithSymbol? MapCashTransaction(string id, JsonObject listEntry, JsonObject detail)
		{
			var type = detail["cashTransactionType"]?.ToString() ?? string.Empty;
			var amount = ParseMoney(detail["amount"], detail);
			if (amount is null)
			{
				throw new CliApiException($"Cash transaction {id} has no amount.");
			}

			var date = GetFilledDate(detail, "cashTransactionHistory");
			var description = detail["description"]?.ToString() ?? listEntry["description"]?.ToString();
			var relatedIsin = detail["relatedIsin"]?.ToString() ?? listEntry["relatedIsin"]?.ToString();
			return BuildCashActivity(id, type, amount, date, description, relatedIsin);
		}

		private async Task<List<ActivityWithSymbol>> ScrapeOvernightAccountAsync(string personId, string savingsAccountId, CancellationToken cancellationToken)
		{
			var transactions = new List<JsonObject>();
			string? cursor = null;

			do
			{
				var input = new JsonObject { ["pageSize"] = PageSize };
				if (cursor != null)
				{
					input["cursor"] = cursor;
				}

				var variables = new JsonObject
				{
					["accountId"] = personId,
					["savingsAccountId"] = savingsAccountId,
					["input"] = input
				};

				var data = await _client.QueryAsync(OvernightTransactionsQuery, variables, "OvernightTransactions", cancellationToken);
				var moreTransactions = data["account"]?["savingsAccount"]?["moreTransactions"];
				foreach (var transaction in moreTransactions?["transactions"]?.AsArray() ?? Enumerable.Empty<JsonNode?>())
				{
					if (transaction is JsonObject item)
					{
						transactions.Add(item);
					}
				}

				cursor = moreTransactions?["cursor"]?.ToString();
			} while (!string.IsNullOrWhiteSpace(cursor));

			var activities = new List<ActivityWithSymbol>();
			foreach (var transaction in transactions)
			{
				try
				{
					var activity = MapOvernightTransaction(transaction);
					if (activity != null)
					{
						activities.Add(activity);
					}
				}
				catch (CliApiException ex)
				{
					_logger.LogWarning(ex, "Skipping CLI overnight transaction {TransactionId}: {Message}", transaction["id"]?.ToString(), ex.Message);
				}
			}

			return activities;
		}

		private ActivityWithSymbol? MapOvernightTransaction(JsonObject entry)
		{
			var id = entry["id"]?.ToString();
			if (string.IsNullOrWhiteSpace(id))
			{
				return null;
			}

			var status = entry["status"]?.ToString() ?? string.Empty;
			if (status is not ("SETTLED" or "FILLED"))
			{
				_logger.LogDebug("Skipping CLI overnight transaction {TransactionId} with status {Status}", id, status);
				return null;
			}

			var type = entry["cashTransactionType"]?.ToString() ?? string.Empty;
			var amount = ParseMoney(entry["amount"], entry);
			if (amount is null)
			{
				throw new CliApiException($"Overnight transaction {id} has no amount.");
			}

			if (!TryParseDate(entry["lastEventDateTime"]?.ToString(), out var date))
			{
				throw new CliApiException($"Overnight transaction {id} has no parseable timestamp.");
			}

			return BuildCashActivity(id!, type, amount, date, entry["description"]?.ToString(), entry["relatedIsin"]?.ToString());
		}

		private ActivityWithSymbol? BuildCashActivity(string id, string type, Money amount, DateTime date, string? description, string? relatedIsin)
		{
			Activity? activity = type switch
			{
				"DEPOSIT" => new CashDepositActivity { Amount = amount, Date = date, TransactionId = id },
				"WITHDRAWAL" => new CashWithdrawalActivity { Amount = amount, Date = date, TransactionId = id },
				"DISTRIBUTION" => new DividendActivity { Amount = amount, Quantity = 0m, Fees = [], Taxes = [], Date = date, TransactionId = id },
				"CASH_TRANSFER_OUT" => null,
				"INTEREST" or "INTEREST_PAYMENT" => new InterestActivity { Amount = amount, Date = date, TransactionId = id },
				_ => null
			};

			if (activity is null)
			{
				_logger.LogWarning("Ignoring unsupported CLI cash transaction type '{Type}' ({TransactionId})", type, id);
				return null;
			}

			return new ActivityWithSymbol
			{
				Activity = activity,
				Symbol = string.IsNullOrWhiteSpace(relatedIsin) ? default! : relatedIsin,
				SymbolName = description,
				ISIN = string.IsNullOrWhiteSpace(relatedIsin) ? null : relatedIsin
			};
		}

		private static DateTime GetFilledDate(JsonObject detail, string historyKey)
		{
			var history = detail[historyKey]?.AsArray();
			if (history != null)
			{
				foreach (var state in new[] { "FILLED", "SETTLED" })
				{
					var entry = history.FirstOrDefault(h => h?["state"]?.ToString() == state);
					if (entry is not null && TryParseDate(entry?["time"]?["time"]?.ToString(), out var date))
					{
						return date;
					}
				}

				foreach (var entry in history)
				{
					if (TryParseDate(entry?["time"]?["time"]?.ToString(), out var date))
					{
						return date;
					}
				}
			}

			if (TryParseDate(detail["lastEventDateTime"]?.ToString(), out var lastEventAt))
			{
				return lastEventAt;
			}

			throw new CliApiException($"Transaction {detail["id"]} has no parseable timestamp.");
		}

		private static bool TryParseDate(string? value, out DateTime date)
		{
			date = default;
			if (string.IsNullOrWhiteSpace(value))
			{
				return false;
			}

			var formats = new[] { "yyyy-MM-dd'T'HH:mm:ss", "dd/MM/yyyy HH:mm:ss" };
			if (DateTime.TryParseExact(value, formats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var exact))
			{
				date = exact;
				return true;
			}

			// Wall-clock parity with the other scraper paths: no timezone conversion for local timestamps; Z-suffixed UTC values convert to UTC.
			return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out date);
		}

		private static List<Money> CollectAmounts(string currency, params JsonNode?[] nodes)
		{
			var result = new List<Money>();
			foreach (var node in nodes)
			{
				if (node is null || node.GetValueKind() == System.Text.Json.JsonValueKind.Null)
				{
					continue;
				}

				var value = ParseDecimal(node);
				if (value is not null && value.Value != 0m)
				{
					result.Add(new Money(currency, value.Value));
				}
			}

			return result;
		}

		private static decimal? ParseDecimal(JsonNode? node)
		{
			var text = node?.ToString();
			if (string.IsNullOrWhiteSpace(text))
			{
				return null;
			}

			return decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value) ? value : null;
		}

		private static Money? ParseMoney(JsonNode? node, JsonObject detail)
		{
			var text = node?.ToString();
			if (string.IsNullOrWhiteSpace(text))
			{
				return null;
			}

			if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out var value))
			{
				throw new CliApiException($"Transaction {detail["id"]} has unparseable amount '{text}'.");
			}

			var currency = detail["currency"]?.ToString() ?? "EUR";
			return new Money(currency, value);
		}
	}
}
