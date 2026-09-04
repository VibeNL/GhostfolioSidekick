# Scalable Capital MCP scraper

The ScraperUtilities tool can fetch Scalable Capital transactions through the official
[Scalable Capital MCP server](https://mcp.scalable.capital/mcp) instead of (or in addition to)
the Playwright browser scrape. Both paths produce the same `Dictionary<int, IEnumerable<ActivityWithSymbol>>`
and are saved with the same CSV helper, so output files (`ScalableCapital_N.csv`) are interchangeable.

Select **3 - Scrape Scalable Capital via MCP** in the tool menu to use this path. It does not need
a Chrome/CDP connection; it only needs network access and one interactive login (see below).

Note: the tool still starts a minimized Chrome instance on startup for the Playwright paths and
terminates it on exit — that window is unused by the MCP path and can be ignored.

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
| cash, `DEPOSIT` | `CashDepositActivity` | Amount taken as-is (positive). |
| cash, `WITHDRAWAL` | `CashWithdrawalActivity` | Amount is negative in the API and used as-is. |
| cash, `CASH_TRANSFER_OUT` (internal transfer) | skipped + warning log | Parity with Playwright: internal transfers are ignored. |
| cash, `DISTRIBUTION` with `relatedIsin` | `DividendActivity` | Quantity 0; ISIN/Symbol from `relatedIsin`. |
| cash, `INTEREST` / `INTEREST_PAYMENT` | `InterestActivity` | — |
| anything else (unknown type, other status) | skipped + warning log | Keeps CSV output compatible with the Playwright path. |

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
- The MCP API does not expose the broker's "account" grouping used by the Playwright path, so `Account` stays null on all activities (the CSV writer ignores it anyway).
- Transactions with status `PENDING`/`CANCELLED` are skipped; a cancelled order that later re-filled appears only once.
- If Scalable Capital changes the MCP tool names or payload shapes, mapping errors surface as per-transaction warnings and skipped rows rather than a crash.
