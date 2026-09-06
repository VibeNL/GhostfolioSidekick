---
name: scalable-cli-sync
description: >
  Incorporate changes from the official Scalable Capital CLI (Rust,
  https://github.com/ScalableCapital/scalable-cli) into our C# port at
  Tools/ScraperUtilities/CliApi/. Use when user asks to "sync scalable-cli",
  "update the CLI API scraper from upstream", "port changes from the official
  Scalable Capital CLI", or reports a bug that exists in both.
---

Sync upstream `ScalableCapital/scalable-cli` (Rust) into our C# port. Our port exists because the MCP server path misses overnight transaction details and no Windows binary of the Rust CLI is published — we maintain the port, so upstream changes must be adopted manually.

## Scope

- Upstream: https://github.com/ScalableCapital/scalable-cli (Rust, `src/*.rs`)
- Our port: `Tools/ScraperUtilities/CliApi/` (C#, .NET 10)
- Wiring: `Tools/ScraperUtilities/ScraperService.cs` — menu option "3. Scalable Capital (CLI API)", branch order CLI → MCP → browser

## File Map (Rust → C#)

| Upstream module | Our file | What lives there |
|---|---|---|
| `auth.rs`, `token.rs`, `session.rs` | `CliTokenProvider.cs`, `CliTokenStore.cs` | Device-code OAuth flow, token refresh rotation, claim extraction, persisted session (`cli-tokens.json`) |
| `dpop*.rs` (mod + software impl) | `DpopKey.cs` | ES256 key gen/load, DPoP proof JWT construction, JWK embedding |
| `graphql.rs`, broker/overnight query execution | `CliGraphqlClient.cs` | GraphQL POST transport, DPoP nonce retry, 401 force-refresh-retry-once (`execute_with_refresh_retry` equivalent) |
| `broker_queries.rs`, `overnight_queries.rs` | `CliScraper.cs` (verbatim query strings) | The 5 GraphQL queries: ResolveBrokerIds, BrokerTransactions, BrokerTransactionDetails, DiscoverOvernightAccounts, OvernightTransactions; cursor paging pageSize 100 |
| broker/overnight row mapping + shared/channel types | `CliScraper.cs` (mapping code) | Transaction → model mapping. **Must stay identical to `Mcp/McpScraper.cs`** for fee/tax/cash handling — port any upstream mapping change into both scrapers |

## Runbook

### Phase 1: Fetch upstream source

1. List upstream files (do not clone the whole repo):
   ```
   gh api repos/ScalableCapital/scalable-cli/git/trees/main?recursive=1 --jq '.tree[].path'
   ```
2. Fetch only the modules relevant to the change via raw URLs:
   `https://raw.githubusercontent.com/ScalableCapital/scalable-cli/main/src/<file>.rs`
3. Save fetched files into the session workspace (e.g. `files/scalable-cli-upstream/`) for diffing — never commit them to this repo.

### Phase 2: Diff per area, in priority order

1. **GraphQL queries** — must stay verbatim with upstream (field names, args, pagination shape). Any query text change is a hard requirement to port.
2. **Endpoints & constants** — issuer `https://secure.scalable.capital` (`/oauth/device/code`, `/oauth/token`), GraphQL `https://de.scalable.capital/api/cli/graphql`, audience `https://de.scalable.capital/api-gateway`, client_id, scope. Upstream constant changes break login silently — check first.
3. **Auth flow** — device-code polling states (`authorization_pending`, `slow_down`, `access_denied`, `expired_token`), refresh-token rotation (new refresh token must be persisted), claim extraction (`person_id`, `session_id`).
4. **Retry semantics** — Rust wraps every GraphQL call in `execute_with_refresh_retry`: 401 → force session invalidation → re-login/refresh → retry once. Our equivalent is the attempt loop in `CliGraphqlClient.QueryAsync` (DPoP-nonce challenge first, then plain 401). Keep both layers.
5. **Row mapping** — new transaction types, fee/tax/cash fields, overnight interest handling (`status` SETTLED/FILLED, cashTransactionType INTEREST/INTEREST_PAYMENT).

### Phase 3: Port to C#

- Follow repo conventions: tabs (width 4), CRLF, UTF-8, `TreatWarningsAsErrors=true`, nullability on.
- Keep recovery paths self-healing: corrupt token file → broad catch → interactive re-login; corrupt signing key → broad catch → fresh key. Do not narrow these catches when porting.
- Unknown transaction types: skip with `LogWarning` (matches Rust default unfiltered scrape — `type_filter`/`status` are optional user CLI args upstream, not defaults).

### Phase 4: Validate

1. `dotnet build` — 0 errors; baseline is 3 warnings (WASM0001 x2, ASPIRE010)
2. `dotnet test` — all pass. **IntegrationTests need Docker Desktop running** (`docker info` to check; start via `Start-Process "C:\Program Files\Docker\Docker\Docker Desktop.exe"` if down). Full suite takes 3–5+ min — run async with long wait.
3. Commit: caveman-commit style + `Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>` trailer

## Intentional Divergences (do NOT "fix")

- **`ECDsa.SignData(data, HashAlgorithmName.SHA256)` is correct as-is.** On .NET 10 it returns raw `r||s` — exactly 64 bytes for P-256 — which IS the JWS ES256 format DPoP requires. Verified empirically (3 live BuildProof calls, first byte never `0x30`). The `DSASignatureFormat.Raw` enum member does not exist in this SDK; any "fix" toward DER encoding breaks login.
- **DPoP proof header embeds the full JWK** (`kty`, `crv`, `x`, `y`) — matches upstream software-key behavior, required because we have no server-side key registration.
- **Key/token files under `%LOCALAPPDATA%\GhostfolioSidekick\`**: `cli-auth-signing-key.json` (minimal JWK kty/crv/d), `cli-tokens.json`. Broad-catch self-heal on load is deliberate.
- **No Windows binary concern** — we are the implementation; upstream platform-specific code (e.g. browser-open, OS keyring) maps to our console + file-based equivalents.

## Parity Reference

`Tools/ScraperUtilities/Mcp/McpScraper.cs` is the fee/tax/cash mapping reference. If an upstream change alters how fees, taxes, or cash movements are derived, apply it to **both** `CliScraper.cs` and `McpScraper.cs` so menu options 2 and 3 produce identical results for the same data.

## Boundaries

- Only touch `Tools/ScraperUtilities/CliApi/*`, `ScraperService.cs` wiring, and (for mapping parity) `Mcp/McpScraper.cs`.
- Do not vendor Rust source into this repo — fetch to session workspace only.
- If upstream changes the OAuth client_id or flow fundamentally (e.g. new grant type), flag it for user review before porting — that is a breaking change, not a sync.
