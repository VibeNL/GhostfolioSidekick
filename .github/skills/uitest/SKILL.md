---
name: uitest
description: >
  GhostfolioSidekick UI test execution and triage skill for
  PortfolioViewer.WASM.UITests (Playwright + xUnit). Use when user asks
  to run UI tests, debug Playwright failures, add/adjust UI tests, or
  investigate page-level regressions in PortfolioViewer.
---

Run and maintain UI tests for GhostfolioSidekick with repo-specific workflow.

## Scope

Primary target:
- `PortfolioViewer/PortfolioViewer.WASM.UITests`

Core files/patterns:
- `PlaywrightTestBase.cs` for browser lifecycle, login + sync setup, screenshots/videos, console logging.
- `CustomWebApplicationFactory.cs` for Kestrel host, in-memory SQLite, seeded data, WASM publish-to-wwwroot bootstrap.
- `WebApplicationFactoryCollection.cs` disables parallelization for shared DB safety.
- `PageObjects/BasePageObject.cs` for shared `WaitForPageLoadAsync` (spinner hidden → any stable state), `ExecuteWithErrorCheckAsync` (auto Blazor error check), `CheckForBlazorErrorAsync`.
- `PageObjects/*` for page objects — all use `NavigateDirectAsync(string? relativePath = null, CancellationToken ct = default)`.
- Tests use `[RetryFact]` from `xRetry.v3`.

## Trigger

Use this skill when user asks any of:
- "run UI tests", "run Playwright tests", "UITest", "UI regression"
- "debug failing PortfolioViewer page test"
- "add UI test for page X"
- "why does Playwright test fail/flaky"

## Runbook

1. Ensure Playwright browsers installed (if first run or browser missing):
   - Build test project so script exists:
     - `dotnet build PortfolioViewer\PortfolioViewer.WASM.UITests\PortfolioViewer.WASM.UITests.csproj`
   - Install browsers:
     - `pwsh PortfolioViewer\PortfolioViewer.WASM.UITests\bin\Debug\net10.0\playwright.ps1 install`

2. Run focused UI tests first:
   - `dotnet test PortfolioViewer\PortfolioViewer.WASM.UITests\PortfolioViewer.WASM.UITests.csproj --filter "<pattern>"`

3. Run full UI suite if needed:
   - `dotnet test PortfolioViewer\PortfolioViewer.WASM.UITests\PortfolioViewer.WASM.UITests.csproj`

4. Triage failures with artifacts:
   - Screenshots: `playwright-screenshots/`
   - Videos: `playwright-videos/<TestName>/`
   - Browser console lines prefixed `[Browser Console]`
   - Optional HTML capture from `CaptureErrorStateAsync()`

5. Debug/verify with screenshots:
   - Open newest `*-error-*.png` first for failed test.
   - Compare screenshot with expected page state after `SetupAsync()` (logged-in, sync complete, page title/primary widget visible).
   - If selector assertion fails, verify target element exists in screenshot before changing waits/selectors.
   - If screenshot shows blank/partial render, correlate with `[Browser Console]` errors and `#blazor-error-ui`.
   - Keep screenshot path in failure summary so repro/debug starts from same visual state.

## Authoring Rules

- Reuse `PlaywrightTestBase` and call `SetupAsync()` unless test intentionally targets pre-login behavior.
- Prefer page objects under `PageObjects/`; avoid raw selectors in test classes unless truly one-off.
- Add stable waits (`WaitForSelectorAsync`, `WaitForURLAsync`) over sleeps/timeouts.
- Keep assertions behavior-focused (visible state, no error UI, expected records loaded).
- Preserve collection fixture model; do not enable parallel execution for this suite.
- Use `base.WaitForPageLoadAsync([selectors], timeout, ct)` from `BasePageObject` — it waits for spinner hidden then any stable state selector.
- Use `ExecuteWithErrorCheckAsync()` for navigation actions to auto-catch Blazor errors.
- All page objects use `NavigateDirectAsync(string? relativePath = null, CancellationToken ct = default)` — pass full URL for absolute paths.
- In test env, pages may render `.alert-danger` when Ghostfolio API is not configured — assertions must accept error state as valid render.

## Investigation Checklist

When UI test fails, check in order:
1. API host bootstrapped (`CustomWebApplicationFactory` started, dynamic URL assigned).
2. WASM static files published/copied to API `wwwroot`.
3. Login path/token flow (`LoginPage`, test token, auth health endpoint).
4. Sync completion gate (Sync button enabled again).
5. Screenshot evidence from `playwright-screenshots/` (actual visible UI state).
6. Page-specific selectors and expected seeded data (`TestDataSeeder`).
7. Blazor error UI presence (`#blazor-error-ui`).
8. `WaitForPageLoadAsync` timeout — check if page renders error state (`.alert-danger`) instead of expected selectors.

## Boundaries

- This skill handles UI-test execution, debugging, and UI-test code changes only.
- Do not change unrelated backend/domain logic unless UI failure root-cause requires minimal coupled fix.
