# GhostfolioSidekick - Copilot Instructions

## Repository Overview

**GhostfolioSidekick** is a .NET 10.0 application that serves as a companion tool (sidecar) for [Ghostfolio](https://github.com/ghostfolio/ghostfolio), a wealth management platform. The repository contains:

1. **GhostfolioSidekick** - A continuously-running Docker container that automatically imports transactions from brokers & crypto exchanges into Ghostfolio, checking hourly for updates
2. **PortfolioViewer** - A Blazor WebAssembly application with .NET Aspire integration for portfolio analysis and visualization
3. **AI Components** - AI-powered features using Semantic Kernel and Web LLM for chat capabilities
4. **Supporting Libraries** - Parsers, database access, API integrations, and utilities

**Repository Size**: ~45 projects in a single solution
**Primary Languages**: C# 14.0 (preview)
**Target Framework**: .NET 10.0 (preview)
**Key Technologies**: Blazor WebAssembly, .NET Aspire, Entity Framework Core, Playwright (UI testing), xUnit

## Build & Validation

### Prerequisites

- **.NET 10 SDK** (version 10.0.102 or later)
- **WASM workload**: Install with `dotnet workload install wasm-tools` before first build
- **Playwright** (for UI tests): Browsers are installed during test runs
- **Mono** (Linux CI only): Required for some build tasks
- **Node.js**: Required for TypeScript compilation and Playwright

### Build Commands

**Standard build** - Builds all 45 projects in the solution:
```bash
dotnet build
```

**Expected outcome**: Build succeeds in ~15 seconds with up to 2 warnings (acceptable). All warnings are treated as errors via `Directory.Build.props` setting `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.

**Important**: Always run `dotnet workload restore` after cloning or if WASM projects fail to build with workload-related errors.

### Testing

**Run all tests**:
```bash
dotnet test
```

**Run tests with coverage** (as done in CI):
```bash
dotnet tool install --global dotnet-coverage
dotnet-coverage collect "dotnet test" -f xml -o "coverage.xml"
```

**Install Playwright browsers** (required before running UI tests):
```bash
# Install for all test projects
find . -type f -name 'playwright.ps1' | while read script; do
  pwsh "$script" install
done

# Or on Windows PowerShell:
Get-ChildItem -Recurse -Include "playwright.ps1" | ForEach-Object { pwsh $_.FullName install }
```

**Test Projects**: The solution contains extensive unit tests with `*.UnitTests` suffix and integration tests:
- `PortfolioViewer.WASM.UITests` - Playwright-based UI tests (produces screenshots/videos on failure)
- `IntegrationTests` - General integration tests

### Running the Application

**Development (Aspire AppHost)**:
```bash
dotnet run --project PortfolioViewer/PortfolioViewer.AppHost/PortfolioViewer.AppHost.csproj
```
This launches the Aspire dashboard and runs both the API service and Blazor WASM client.

**GhostfolioSidekick Console App**:
```bash
dotnet run --project GhostfolioSidekick/GhostfolioSidekick.csproj
```

**Docker Build** (see `Dockerfile`):
- Uses multi-stage build
- Installs Python, Node.js, wasm-tools workload, and supervisord
- Builds both PortfolioViewer.ApiService, PortfolioViewer.WASM, and GhostfolioSidekick
- Uses platform-specific builds with `TARGETARCH` support

## Project Layout & Architecture

### Repository Structure

```
/
├── .github/workflows/docker-publish.yml    # Main CI/CD pipeline
├── Directory.Build.props                   # TreatWarningsAsErrors=true for entire solution
├── .editorconfig                          # Code style: tabs, CRLF, charset=utf-8
├── Dockerfile                             # Multi-stage build for production
├── GhostfolioSidekick.slnx               # Solution file (XML format)
│
├── GhostfolioSidekick/                   # Main console application (entry point)
├── PortfolioViewer/                      # Blazor WASM viewer application
│   ├── PortfolioViewer.AppHost/         # .NET Aspire orchestration (Program.cs is entry point)
│   ├── PortfolioViewer.WASM/            # Blazor WebAssembly client (uses Aspire4Wasm)
│   ├── PortfolioViewer.WASM.Data/       # Data services, models, and EF Core logic
│   ├── PortfolioViewer.ApiService/      # Backend API service
│   ├── PortfolioViewer.Common/          # Shared types
│   ├── PortfolioViewer.ServiceDefaults/ # Aspire service defaults
│   ├── PortfolioViewer.WASM.AI/         # AI integration (WebLLM chat)
│   └── *UnitTests/                      # Test projects (xUnit)
│
├── AI/                                   # AI components using Semantic Kernel
│   ├── AI.Agents/                       # AI agent implementations
│   ├── AI.Functions/                    # AI function definitions
│   ├── AI.Server/                       # AI server component
│   └── AI.Common/                       # Shared AI logic
│
├── Database/                            # Entity Framework Core with SQLite
├── GhostfolioAPI/                       # Client for Ghostfolio REST API
├── Parsers/                             # Transaction parsers for various brokers
├── Model/                               # Domain models
├── Configuration/                       # Configuration management (JSON-based)
├── ExternalDataProvider/                # Yahoo, CoinGecko, DividendMax integrations
├── PerformanceCalculations/             # Portfolio performance calculations
├── Cryptocurrency/                      # Crypto-specific logic
├── Utilities/                           # Common utilities
└── Tools/                               # Utility tools (AnonymisePDF, ScraperUtilities)
```

### Key Architectural Patterns

1. **Blazor WebAssembly with .NET Aspire**: PortfolioViewer uses Aspire for service orchestration (`PortfolioViewer.AppHost`). The WASM client communicates with `PortfolioViewer.ApiService` backend.

2. **Entity Framework Core with SQLite**: All database operations use `Database/DatabaseContext`. Migrations are in `Database/Migrations/` (excluded from SonarQube analysis).

3. **Repository Pattern**: Database access is abstracted through repository interfaces in `Database.Repository` namespace.

4. **Parser Strategy Pattern**: Each broker/exchange has a dedicated parser in the `Parsers/` directory (e.g., Trading212, DeGiro, ScalableCapital).

5. **Configuration via JSON**: Main configuration in `Configuration/` project. Users provide JSON files mapping currencies, symbols, and account settings.

### Important Configuration Files

- **`.editorconfig`**: Enforces tabs (size 4), CRLF line endings, UTF-8. Follow these conventions strictly.
- **`Directory.Build.props`**: Sets `TreatWarningsAsErrors=true` globally - all warnings block builds.
- **`GlobalSuppressions.cs`**: Code analysis suppressions (root level).
- **`appsettings.json`**: Application settings in `GhostfolioSidekick/` and `PortfolioViewer.ApiService/`.

### Database & Migrations

- **Database**: SQLite via EF Core 10.0
- **Migrations Location**: `Database/Migrations/`
- **Context**: `DatabaseContext` in `Database/` project
- **Add Migration**: `dotnet ef migrations add <name> --project Database`
- **Update Database**: Applications handle migrations at startup

### Testing Infrastructure

- **Framework**: xUnit
- **UI Testing**: Playwright for `PortfolioViewer.WASM.UITests`
- **Playwright Setup**: Run `pwsh <test-project>/bin/Debug/net10.0/playwright.ps1 install` before first UI test run
- **Mocking**: Uses Moq and custom fixtures (see `*UnitTests/` projects)
- **Test Output**: Playwright produces screenshots (`playwright-screenshots/`) and videos (`playwright-videos/`) on failure

## CI/CD Pipeline (.github/workflows/docker-publish.yml)

The main workflow runs on:
- Push to `master` branch
- Pull requests to `master`
- Manual dispatch

**Workflow Steps**:

1. **Checkout**: Fetch full history (`fetch-depth: 0`)
2. **Setup**: JDK 21, .NET 10, Mono, WASM workload
3. **Install Playwright**: `npx playwright install` + per-project installation via `playwright.ps1`
4. **Install Tools**: `dotnet-sonarscanner`, `dotnet-coverage` (global tools)
5. **Build**: `dotnet build` (must succeed without errors)
6. **Test**: `dotnet-coverage collect "dotnet test" -f xml -o "coverage.xml"`
7. **SonarCloud Analysis**: Upload coverage and run static analysis (requires `SONAR_TOKEN`)
8. **Artifacts**: Upload coverage report, Playwright screenshots/videos
9. **Security Scans** (publish job): Check for deprecated/vulnerable packages, run OSV scanner

**Important CI Notes**:
- Tests run after Playwright browser installation across all test projects
- Mono is required on Linux for certain build tasks
- WASM workload must be installed with `dotnet workload restore`
- SonarCloud project: `VibeNL_GhostfolioSidekick` (org: `vibenl`)

## Coding Standards & Conventions

### Style

- **Indentation**: Use TABS (not spaces), width 4
- **Line Endings**: CRLF (Windows-style)
- **Charset**: UTF-8
- **Naming**: PascalCase for types/methods, interfaces start with `I`
- **Nullability**: Enabled (`<Nullable>enable</Nullable>`)
- **Implicit Usings**: Enabled (`<ImplicitUsings>enable</ImplicitUsings>`)

### Project Naming

All project assemblies follow the pattern: `GhostfolioSidekick.<ProjectName>`
- Configured via `<AssemblyName>GhostfolioSidekick.$(MSBuildProjectName)</AssemblyName>`
- Root namespace: `GhostfolioSidekick.<ProjectName.Replace(" ", "_")>`

### Code Quality

- **Production-Ready Code**: Always produce production-ready code following SOLID principles, DRY (Don't Repeat Yourself), YAGNI (You Aren't Gonna Need It), and other software engineering best practices
- **Verification Required**: Always verify build (`dotnet build`) and tests (`dotnet test`) pass before marking any task as complete
- **Treat Warnings as Errors**: ALL warnings must be resolved before committing
- **SonarCloud**: Code must pass quality gate (see badge in README.md)
- **Exclusions**: JavaScript files and certain generated code excluded from analysis (via `<SonarQubeSetting>` in `.csproj` files)

## Common Workarounds & Gotchas

1. **WASM Build Issues**: If you get WASM-related errors, always run `dotnet workload restore` first.

2. **Playwright Browser Issues**: If UI tests fail with browser errors, ensure browsers are installed via `playwright.ps1 install` in each test project's bin directory.

3. **Database Migrations**: The application handles migrations automatically at startup. Don't run `dotnet ef database update` manually unless developing migrations.

4. **Crypto Workarounds**: Several crypto-specific workarounds exist (see README.md sections on `use.crypto.workaround.*` settings). These handle edge cases like dust amounts and staking rewards.

5. **Symbol Matching**: `GhostfolioAPI/GhostfolioSymbolMatcher.cs` contains TODO comments about improving symbol matching logic.

6. **Line Ending Issues**: The repository uses CRLF. Git may convert these. Ensure `.gitattributes` is respected.

7. **TypeScript Compilation**: Some projects include TypeScript files (e.g., `PortfolioViewer.WASM/wwwroot/js/site.ts`) compiled via MSBuild using `Microsoft.TypeScript.MSBuild` package.

## Validation Checklist

Before submitting changes, always:

1. ✅ Run `dotnet build` - must succeed with 0 errors (up to 2 warnings acceptable in full build)
2. ✅ Run `dotnet test` - all tests must pass
3. ✅ Check `.editorconfig` compliance - use tabs, CRLF, UTF-8
4. ✅ If modifying WASM projects, verify with `dotnet workload restore`
5. ✅ If modifying UI components, consider running Playwright tests
6. ✅ If adding dependencies, ensure they're compatible with .NET 10.0
7. ✅ Check that code follows existing patterns (repository pattern, service injection, etc.)
8. ✅ For database changes, add migrations and test migration apply/rollback

## Additional Resources

- **README.md**: Comprehensive setup instructions, configuration file format, supported brokers
- **Documentation/Parsers/**: Parser-specific documentation
- **Examples/**: Example configuration files and usage scenarios
- **GitHub Workflow**: `.github/workflows/docker-publish.yml` - definitive build/test/deploy pipeline
- **Dockerfile**: Production deployment configuration with supervisord for multi-process container

---

**Trust these instructions**: Only perform additional searches if information here is incomplete or conflicts with current repository state. This document is comprehensive and validated against the actual repository structure and CI pipeline.
