using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Logging;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Tools.ScraperUtilities;

namespace GhostfolioSidekick.Tools.ScraperUtilities.Mcp
{
	/// <summary>
	/// Scrapes Scalable Capital transactions via the official MCP server instead of browser automation.
	/// Broker portfolios are scraped first, followed by the active overnight savings account when available.
	/// Produces the same output contract as the Playwright-based scraper: account index to activities.
	/// </summary>
	public class McpScraper
	{
		private readonly McpClient _client;
		private readonly ILogger _logger;

		public McpScraper(McpClient client, ILogger logger)
		{
			_client = client;
			_logger = logger;
		}

		public async Task<Dictionary<int, IEnumerable<ActivityWithSymbol>>> ScrapeTransactionsAsync(CancellationToken cancellationToken)
		{
			var result = new Dictionary<int, IEnumerable<ActivityWithSymbol>>();
			var index = 0;

			foreach (var portfolioId in await GetPortfoliosAsync(cancellationToken))
			{
				index++;
				_logger.LogInformation("Scraping Scalable Capital MCP transactions for portfolio {PortfolioId}", portfolioId);
				result[index] = await ScrapePortfolioAsync(portfolioId, cancellationToken);
			}

			var overnightActivities = await ScrapeOvernightAccountAsync(cancellationToken);
			if (overnightActivities != null)
			{
				index++;
				result[index] = overnightActivities;
			}

			if (result.Count == 0)
			{
				throw new McpException("No accessible Scalable Capital portfolios or overnight accounts were returned by the MCP server.");
			}

			return result;
		}

		private async Task<List<string>> GetPortfoliosAsync(CancellationToken cancellationToken)
		{
			var response = await _client.CallToolAsync("list_accessible_portfolios", new JsonObject(), cancellationToken);
			var portfolios = response["portfolios"]?.AsArray();
			if (portfolios is null)
			{
				throw new McpException("MCP 'list_accessible_portfolios' returned no portfolio list.");
			}

			return portfolios.Select(p => p?["portfolioId"]?.ToString()).Where(id => !string.IsNullOrWhiteSpace(id)).Select(id => id!).ToList();
		}

		private async Task<IEnumerable<ActivityWithSymbol>> ScrapePortfolioAsync(string portfolioId, CancellationToken cancellationToken)
		{
			var transactions = new List<JsonObject>();
			string? cursor = null;

			do
			{
				var arguments = new JsonObject { ["portfolioId"] = portfolioId, ["pageSize"] = 100 };
				if (cursor != null)
				{
					arguments["cursor"] = cursor;
				}

				var page = await _client.CallToolAsync("list_portfolio_transactions", arguments, cancellationToken);
				foreach (var transaction in page["transactions"]?.AsArray() ?? Enumerable.Empty<JsonNode?>())
				{
					if (transaction is JsonObject item)
					{
						transactions.Add(item);
					}
				}

				cursor = page["page"]?["nextCursor"]?.ToString();
			} while (!string.IsNullOrWhiteSpace(cursor));

			var activities = new List<ActivityWithSymbol>();
			foreach (var transaction in transactions)
			{
				try
				{
					var activity = await MapTransactionAsync(portfolioId, transaction, cancellationToken);
					if (activity != null)
					{
						activities.Add(activity);
					}
				}
				catch (McpException ex)
				{
					_logger.LogWarning(ex, "Skipping MCP transaction {TransactionId}: {Message}", transaction["id"]?.ToString(), ex.Message);
				}
			}

			return activities;
		}

		private async Task<IEnumerable<ActivityWithSymbol>?> ScrapeOvernightAccountAsync(CancellationToken cancellationToken)
		{
			var transactions = new List<JsonObject>();
			string? cursor = null;

			try
			{
				do
				{
					var arguments = new JsonObject { ["pageSize"] = 100 };
					if (cursor != null)
					{
						arguments["cursor"] = cursor;
					}

					var page = await _client.CallToolAsync("list_overnight_transactions", arguments, cancellationToken);
					foreach (var transaction in page["transactions"]?.AsArray() ?? Enumerable.Empty<JsonNode?>())
					{
						if (transaction is JsonObject item)
						{
							transactions.Add(item);
						}
					}

					cursor = page["page"]?["nextCursor"]?.ToString();
				} while (!string.IsNullOrWhiteSpace(cursor));
			}
			catch (McpException ex)
			{
				// The MCP server has no tool to enumerate overnight accounts, so this covers both "no active account"
				// and "multiple active accounts without an explicit savingsAccountId".
				_logger.LogWarning(ex, "Skipping Scalable Capital overnight transactions: {Message}", ex.Message);
				return null;
			}

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
				catch (McpException ex)
				{
					_logger.LogWarning(ex, "Skipping MCP overnight transaction {TransactionId}: {Message}", transaction["id"]?.ToString(), ex.Message);
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
				_logger.LogDebug("Skipping MCP overnight transaction {TransactionId} with status {Status}", id, status);
				return null;
			}

			if (entry["isCancellation"]?.GetValueKind() == JsonValueKind.True)
			{
				_logger.LogDebug("Skipping cancelled MCP overnight transaction {TransactionId}", id);
				return null;
			}

			var type = entry["cashTransactionType"]?.ToString() ?? string.Empty;
			var amount = ParseMoney(entry["amount"], entry);
			if (amount is null)
			{
				throw new McpException($"Overnight transaction {id} has no amount.");
			}

			if (!TryParseDate(entry["lastEventAt"]?.ToString(), out var date))
			{
				throw new McpException($"Overnight transaction {id} has no parseable timestamp.");
			}

			return BuildCashActivity(id, type, amount, date, entry["description"]?.ToString(), entry["relatedIsin"]?.ToString());
		}

		private async Task<ActivityWithSymbol?> MapTransactionAsync(string portfolioId, JsonObject transaction, CancellationToken cancellationToken)
		{
			var id = transaction["id"]?.ToString();
			if (string.IsNullOrWhiteSpace(id))
			{
				return null;
			}

			var status = transaction["status"]?.ToString() ?? string.Empty;
			if (status is not ("SETTLED" or "FILLED"))
			{
				_logger.LogDebug("Skipping MCP transaction {TransactionId} with status {Status}", id, status);
				return null;
			}

			var details = await _client.CallToolAsync("get_transaction_details", new JsonObject { ["portfolioId"] = portfolioId, ["transactionId"] = id }, cancellationToken);
			if (details["transaction"] is not JsonObject detail)
			{
				throw new McpException($"Transaction {id} has no detail payload.");
			}

			var kind = detail["kind"]?.ToString();
			return kind switch
			{
				"security_trade" => MapSecurityTrade(detail),
				"cash" => MapCashTransaction(id, transaction, detail),
				"non_trade_security" => SkipNonTradeSecurity(id),
				_ => throw new McpException($"Unknown transaction kind '{kind}'.")
			};

		}

		private ActivityWithSymbol? SkipNonTradeSecurity(string id)
		{
			_logger.LogWarning("Ignoring unsupported MCP non-trade security transaction {TransactionId}", id);
			return null;
		}

		private ActivityWithSymbol? MapSecurityTrade(JsonObject detail)
		{
			var security = detail["security"];
			var isin = security?["isin"]?.ToString();
			if (string.IsNullOrWhiteSpace(isin))
			{
				throw new McpException("Security transaction has no ISIN.");
			}

			var trade = detail["securityTrade"];
			var side = trade?["side"]?.ToString() ?? string.Empty;
			var quantity = ParseDecimal(trade?["numberOfShares"]?["filled"]);
			var unitPrice = ParseMoney(trade?["averagePrice"], detail);
			if (quantity is null || quantity <= 0m || unitPrice is null)
			{
				throw new McpException($"Security transaction {detail["id"]} has no filled quantity or price.");
			}

			var date = GetFilledDate(detail);
			var currency = detail["currency"]?.ToString() ?? "EUR";
			var fees = CollectAmounts(currency, trade?["fee"], trade?["transactionalFee"], trade?["tradeTransactionAmounts"]?["transactionFee"], trade?["tradeTransactionAmounts"]?["venueFee"]);
			var taxes = CollectAmounts(currency, trade?["taxes"], trade?["tradeTransactionAmounts"]?["taxAmount"]);

			Activity activity = side switch
			{
				"BUY" => new BuyActivity { Quantity = quantity.Value, UnitPrice = unitPrice, Fees = fees, Taxes = taxes, Date = date, TransactionId = detail["id"]?.ToString() ?? string.Empty },
				"SELL" => new SellActivity { Quantity = quantity.Value, UnitPrice = unitPrice, Fees = fees, Taxes = taxes, Date = date, TransactionId = detail["id"]?.ToString() ?? string.Empty },
				_ => throw new McpException($"Unknown trade side '{side}'.")
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
			var cash = detail["cash"];
			if (cash is null)
			{
				throw new McpException($"Cash transaction {id} has no cash payload.");
			}

			var type = cash["transactionType"]?.ToString() ?? string.Empty;
			var amount = ParseMoney(cash["amount"], detail);
			if (amount is null)
			{
				throw new McpException($"Cash transaction {id} has no amount.");
			}

			var date = GetFilledDate(detail);
			var description = cash["description"]?.ToString() ?? listEntry["description"]?.ToString();
			var relatedIsin = cash["relatedIsin"]?.ToString() ?? listEntry["cash"]?["relatedIsin"]?.ToString();

			return BuildCashActivity(id, type, amount, date, description, relatedIsin);
		}

		private ActivityWithSymbol? BuildCashActivity(string id, string type, Money amount, DateTime date, string? description, string? relatedIsin)
		{
			Activity? activity = type switch
			{
				"DEPOSIT" => new CashDepositActivity { Amount = amount, Date = date, TransactionId = id },
				"WITHDRAWAL" => new CashWithdrawalActivity { Amount = amount, Date = date, TransactionId = id },
				"DISTRIBUTION" => new DividendActivity { Amount = amount, Quantity = 0m, Fees = [], Taxes = [], Date = date, TransactionId = id },
				"CASH_TRANSFER_OUT" or "CASH_TRANSFER_IN" => null,
				"INTEREST" or "INTEREST_PAYMENT" => new InterestActivity { Amount = amount, Date = date, TransactionId = id },
				_ => null
			};

			if (activity is null)
			{
				_logger.LogWarning("Ignoring unsupported MCP cash transaction type '{Type}' ({TransactionId})", type, id);
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

		private static DateTime GetFilledDate(JsonObject detail)
		{
			var history = detail["history"]?.AsArray();
			if (history != null)
			{
				foreach (var state in new[] { "FILLED", "SETTLED" })
				{
					var entry = history.FirstOrDefault(h => h?["state"]?.ToString() == state);
					if (entry is not null && TryParseDate(entry["timestamp"]?.ToString(), out var date))
					{
						return date;
					}
				}

				foreach (var entry in history)
				{
					if (TryParseDate(entry?["timestamp"]?.ToString(), out var date))
					{
						return date;
					}
				}
			}

			if (TryParseDate(detail["lastEventAt"]?.ToString(), out var lastEventAt))
			{
				return lastEventAt;
			}

			throw new McpException($"Transaction {detail["id"]} has no parseable timestamp.");
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

			// Wall-clock parity with the Playwright path: no timezone conversion for local timestamps; Z-suffixed UTC values convert to UTC.
			return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out date);
		}

		private static List<Money> CollectAmounts(string currency, params JsonNode?[] nodes)
		{
			var result = new List<Money>();
			foreach (var node in nodes)
			{
				if (node is null || node.GetValueKind() == JsonValueKind.Null)
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
				throw new McpException($"Transaction {detail["id"]} has unparseable amount '{text}'.");
			}

			var currency = detail["currency"]?.ToString() ?? "EUR";
			return new Money(currency, value);
		}
	}
}
