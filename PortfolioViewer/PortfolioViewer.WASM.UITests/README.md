# Portfolio Viewer WASM UI Tests

This project contains end-to-end UI tests for the Portfolio Viewer WASM application using Playwright.

## Architecture

### Page Object Pattern

The tests use the Page Object pattern to encapsulate page interactions and improve maintainability. Each page in the application has a corresponding page object class.

#### Page Objects

- **LoginPage** (`PageObjects/LoginPage.cs`)
  - Handles login form interactions
  - Methods: `LoginAsync()`, `FillAccessTokenAsync()`, `WaitForSuccessfulLoginAsync()`
  
- **HomePage** (`PageObjects/HomePage.cs`)
  - Handles home page and sync functionality
  - Methods: `ClickSyncButtonAsync()`, `WaitForSyncToCompleteAsync()`, `GetProgressPercentageAsync()`

### Base Test Class

**PlaywrightTestBase** (`PlaywrightTestBase.cs`) provides common functionality:
- Playwright browser setup and teardown
- Screenshot and video recording directories
- Error capture utilities
- Implements `IAsyncLifetime` for proper test lifecycle management

## Test Structure

### WasmUiSmokeTests

1. **Login_ShouldSucceedWithValidToken**
   - Tests the login flow with valid credentials
   - Verifies redirection to home page

2. **HomePage_ShouldDisplaySyncButton**
   - Verifies home page loads correctly after login
   - Checks sync button visibility

3. **Sync_ShouldStartAndComplete**
   - Tests the complete sync workflow
   - Monitors sync progress
   - Verifies sync completion indicators
   - Takes screenshots at different stages (before, during, after sync)

4. **DebugApiHealthEndpoint**
   - Helper test to verify API connectivity

## Running Tests

### Prerequisites

```bash
# Install Playwright browsers (first time only)
pwsh PortfolioViewer/PortfolioViewer.WASM.UITests/bin/Debug/net10.0/playwright.ps1 install
```

### Run All Tests

```bash
dotnet test PortfolioViewer/PortfolioViewer.WASM.UITests
```

### Run Specific Test

```bash
dotnet test --filter "FullyQualifiedName~Sync_ShouldStartAndComplete"
```

## Test Configuration

### Test Access Token

The test server is configured with a mock access token (`test-token-12345`) in `CustomWebApplicationFactory.cs`. This token is automatically used by all tests.

### Timeouts

- **Page Load**: 10 seconds
- **Login**: 10 seconds
- **Sync Completion**: 120 seconds (2 minutes)

## Test Artifacts

Tests automatically capture:

### Screenshots
- Success screenshots in `playwright-screenshots/`
- Error screenshots with `-error-` suffix
- Screenshots at different sync stages (before, during, after)

### Videos
- Recorded in `playwright-videos/`
- Only saved when tests fail

### HTML Snapshots
- Error HTML snapshots with `-error-` suffix
- Useful for debugging layout/rendering issues

## Debugging Tests

### View Browser (Headed Mode)

Modify `PlaywrightTestBase.cs` to run in headed mode:

```csharp
Browser = await Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = false });
```

### Browser Console Logs

All browser console output is automatically captured and written to test output:

```
[Browser Console] log: Application started
[Browser Console] error: Network error
```

### Inspect Test Failures

1. Check error screenshots in `playwright-screenshots/`
2. Review HTML snapshot files
3. Check browser console logs in test output
4. Watch recorded video (if available)

## Best Practices

### Writing New Tests

1. **Inherit from PlaywrightTestBase**
   ```csharp
   public class MyTests(CustomWebApplicationFactory fixture) : PlaywrightTestBase(fixture)
   ```

2. **Use Page Objects**
   ```csharp
   var loginPage = new LoginPage(Page!);
   await loginPage.LoginAsync(ServerAddress, CustomWebApplicationFactory.TestAccessToken);
   ```

3. **Capture Error State**
   ```csharp
   catch (Exception ex)
   {
       await CaptureErrorStateAsync("my-test");
       throw;
   }
   ```

4. **Use Descriptive Screenshots**
   ```csharp
   await Page.ScreenshotAsync(new PageScreenshotOptions { Path = GetScreenshotPath("descriptive-name") });
   ```

### Adding New Page Objects

1. Create class in `PageObjects/` directory
2. Accept `IPage` in constructor
3. Define selector constants
4. Create methods for user interactions
5. Add waiting/assertion helper methods

Example:

```csharp
public class MyPage(IPage page)
{
    private readonly IPage _page = page;
    private const string ButtonSelector = "button#my-button";
    
    public async Task ClickMyButtonAsync()
    {
        await _page.ClickAsync(ButtonSelector);
    }
    
    public async Task WaitForPageLoadAsync(int timeout = 10000)
    {
        await _page.WaitForSelectorAsync(ButtonSelector, new() { Timeout = timeout });
    }
}
```

## CI/CD Integration

Tests run automatically in GitHub Actions CI pipeline:
- Playwright browsers are installed via `playwright.ps1 install`
- Tests produce screenshots and videos as artifacts on failure
- Coverage reports are generated

See `.github/workflows/docker-publish.yml` for CI configuration.

## Troubleshooting

### "Playwright not found" Error
```bash
pwsh PortfolioViewer/PortfolioViewer.WASM.UITests/bin/Debug/net10.0/playwright.ps1 install
```

### "Element not found" Errors
- Check selectors in page objects
- Increase timeouts if page loads slowly
- Review error screenshots for UI changes

### Sync Test Timeouts
- Sync can take up to 2 minutes for first sync
- Subsequent syncs are faster (partial sync)
- Check browser console logs for errors

### WASM Build Issues
```bash
dotnet workload restore
dotnet build
```
