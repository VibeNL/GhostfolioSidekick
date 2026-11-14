# Scalable Capital MCP scraper

The ScraperUtilities tool fetches Scalable Capital transactions through the official
[Scalable Capital MCP server](https://mcp.scalable.capital/mcp). It produces a
`Dictionary<int, IEnumerable<ActivityWithSymbol>>` that is saved with the CSV helper as
`ScalableCapital_N.csv` files (one per account index).

It does not need a browser or CDP connection; it only needs network access and one interactive
login (see below).

# How authentication works

The MCP endpoint is protected with OAuth 2.0 (RFC 9728 / RFC 8414 discovery, PKCE S256, public client):

| Step | What happens |
|---|---|
| Discovery | `WWW-Authenticate` on the MCP endpoint points to the protected-resource metadata; the authorization-server metadata lives at `https://mcp.scalable.capital/.well-known/oauth-authorization-server`. |
| Client registration | On first run the tool dynamically registers a public client (`POST /register`) with redirect URI `http://localhost:<free port>/`. The returned `client_id` is persisted. |
| Authorization code + PKCE | A browser opens the `/authorize` URL (S256 challenge, scopes `openid profile offline_access`). After you log in at Scalable Capital, the provider redirects to the local listener, which captures the code and shows a "you can close this window" page. The code is exchanged for tokens at `/token`. |
| Token storage | `{client_id, refresh_token}` are persisted to `%LOCALAPPDATA%\GhostfolioSidekick\mcp-tokens.json` (Windows) or `~/.local/share/GhostfolioSidekick/mcp-tokens.json` (Linux/macOS). Access tokens are short-lived and always refreshed from the stored refresh token; no access token is written to disk. |
| Refresh | Every run starts with a refresh-token grant. If that fails, the tool falls back to the interactive login again. Refresh responses may rotate the refresh token — the new one replaces the old one when present. |

Login happens once per machine (or until Scalable Capital invalidates the client/refresh token).
The local callback listener binds `localhost` only and is closed as soon as the code arrives or after a 5 minute timeout.

# MCP protocol notes

- Endpoint: `https://mcp.scalable.capital/mcp`, JSON-RPC 2.0 over POST, **stateless** (no initialize handshake).
- Every request needs `Authorization: Bearer <token>`, `Content-Type: application/json` and
  `Accept: application/json, text/event-stream`. A missing `Accept` header is rejected with `-32000 Not Acceptable`.
- Tool calls use `method: "tools/call"`; the tool payload arrives as a JSON string in `.result.content[0].text`, which the client parses again.
- On HTTP 401 the client invalidates its cached token and retries once with a fresh one (covers refresh-token rotation races).

# Data mapping

| MCP input | Activity produced | Notes |
|---|---|---|
| `list_portfolio_transactions` → security, side `BUY`, status `FILLED`/`SETTLED` | `BuyActivity` | Quantity = `securityTrade.numberOfShares.filled`; UnitPrice = `averagePrice` in the transaction currency. |
| same, side `SELL` | `SellActivity` | Same fields as buy. |
| kind `non_trade_security` (e.g. share transfer) | skipped + warning log | Non-trade security transactions are not mapped to an activity type. |
| cash, `DEPOSIT` | `CashDepositActivity` | Amount taken as-is (positive). |
| cash, `WITHDRAWAL` | `CashWithdrawalActivity` | Amount is negative in the API and used as-is. |
| cash, `CASH_TRANSFER_OUT` (internal transfer) | skipped + warning log | Internal transfers are ignored. |
| cash, `DISTRIBUTION` with `relatedIsin` | `DividendActivity` | Quantity 0; ISIN/Symbol from `relatedIsin`. |
| cash, `INTEREST` / `INTEREST_PAYMENT` | `InterestActivity` | — |
| anything else (unknown type, other status) | skipped + warning log | Unsupported types never abort the scrape. |

## Overnight savings account

In addition to the broker portfolios, the scraper fetches the active overnight savings account via
`list_overnight_transactions` (paginated with `page.nextCursor`). It is emitted as the next account
index after all broker portfolios: a user with one broker portfolio and an overnight account gets
`ScalableCapital_1.csv` (broker) and `ScalableCapital_2.csv` (overnight).

| MCP input | Activity produced | Notes |
|---|---|---|
| `list_overnight_transactions` → `DEPOSIT`, status `SETTLED`/`FILLED` | `CashDepositActivity` | Amount taken as-is (positive in the API). |
| same, `WITHDRAWAL` | `CashWithdrawalActivity` | Amount taken as-is (positive in the API). |
| same, `INTEREST` / `INTEREST_PAYMENT` | `InterestActivity` | Monthly overnight interest payouts. |
| same, `DISTRIBUTION` with `relatedIsin` | `DividendActivity` | Quantity 0; ISIN/Symbol from `relatedIsin`. |
| same, `CASH_TRANSFER_IN` / `CASH_TRANSFER_OUT` (internal transfer) | skipped + warning log | Internal broker↔overnight transfers are ignored on both sides. |
| anything else (unknown type, other status, cancelled entry) | skipped + warning/debug log | Unsupported types never abort the scrape. |

Overnight dates come from `lastEventAt` (ISO 8601 UTC). The MCP server has no tool to enumerate
overnight accounts: the call is made without an explicit `savingsAccountId` and relies on
auto-resolution of a single active account. If the user has several active overnight accounts (or
none), the call fails, the scraper logs a warning and skips the overnight part instead of crashing.

Fees/taxes: the detail payload carries several fee fields (`fee`, `transactionalFee`,
`tradeTransactionAmounts.transactionFee/venueFee`) and tax fields (`taxes`,
`tradeTransactionAmounts.taxAmount`). All non-null, non-zero values are collected into the activity's
`Fees`/`Taxes` lists; the CSV writer sums them.

Date: taken from the transaction `history` — first an entry with state `FILLED`, then one with `SETTLED`,
then any entry, falling back to `lastEventAt`. History timestamps are not consistently formatted by the API
(ISO 8601 UTC or local `dd/MM/yyyy HH:mm:ss`), so parsing tries both and normalizes to UTC.

All numeric values in the MCP payload are JSON strings; they are parsed with invariant culture.

# Known limitations

- One login per machine; tokens live in a plain JSON file under your user profile (same trust level as other local tool state).
- The MCP API does not expose the broker's "account" grouping, so `Account` stays null on all activities (the CSV writer ignores it anyway).
- Only one active overnight account is scraped (auto-resolved by the MCP server); if several are active, the overnight part is skipped with a warning.
- Transactions with status `PENDING`/`CANCELLED` are skipped; a cancelled order that later re-filled appears only once.
- If Scalable Capital changes the MCP tool names or payload shapes, mapping errors surface as per-transaction warnings and skipped rows rather than a crash.
