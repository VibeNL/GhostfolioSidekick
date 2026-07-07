# uitest

GhostfolioSidekick Playwright UI test skill for `PortfolioViewer.WASM.UITests`.

## What it does

Runs and debugs UI tests using repo conventions: `PlaywrightTestBase`, `CustomWebApplicationFactory`, page objects, retry-based facts, and seeded in-memory SQLite test data.

Focus areas:
- Run focused/full Playwright UI tests
- Diagnose failures with screenshot/video/console artifacts
- Debug and verify expected UI state from captured screenshots
- Add or update page-object-based UI tests safely
- Reduce flakiness with stable waits and fixture-consistent patterns

## How to invoke

Trigger naturally with:
- "run UI tests"
- "debug Playwright failure"
- "add UITest for holdings/accounts/etc."
- "investigate UI regression in PortfolioViewer"

## Repo-specific notes

- Test project: `PortfolioViewer\PortfolioViewer.WASM.UITests\PortfolioViewer.WASM.UITests.csproj`
- Browser install script: `PortfolioViewer\PortfolioViewer.WASM.UITests\bin\Debug\net10.0\playwright.ps1`
- Diagnostics artifacts:
  - `playwright-screenshots/`
  - `playwright-videos/<TestName>/`
  - browser console logs (`[Browser Console] ...`)

Quick verify flow:
- Check newest `*-error-*.png` for actual rendered UI
- Confirm expected state (login/sync/page title/table/widget)
- Use screenshot evidence before modifying selectors or waits

## See also

- [`SKILL.md`](./SKILL.md) — full LLM-facing instructions
- [`../../copilot-instructions.md`](../../copilot-instructions.md) — project build/test context
