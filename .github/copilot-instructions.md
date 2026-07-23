# GhostfolioSidekick - Copilot Instructions

## Repository Overview

**GhostfolioSidekick**: .NET 10.0 sidecar for [Ghostfolio](https://github.com/ghostfolio/ghostfolio) (wealth mgmt platform). Contains:

1. **GhostfolioSidekick** - Docker container, auto-imports broker/crypto transactions to Ghostfolio hourly
2. **PortfolioViewer** - Blazor WebAssembly + .NET Aspire for portfolio analysis/visualization
3. **AI Components** - Semantic Kernel + Web LLM for chat
4. **Supporting Libraries** - Parsers, DB, API integrations, utilities

**Size**: ~45 projects, single solution  
**Lang**: C# 14.0 (preview)  
**Framework**: .NET 10.0 (preview)  
**Tech**: Blazor WebAssembly, .NET Aspire, EF Core, Playwright (UI testing), xUnit

## Build & Validation

### Prerequisites

- **.NET 10 SDK** (10.0.102+)
- **WASM workload**: `dotnet workload install wasm-tools` before first build
- **Playwright**: Browsers install during test runs
- **Mono** (Linux CI only): Some build tasks
- **Node.js**: TypeScript + Playwright

### Build Commands

Build all 45 projects:
dotnet build
**Expected**: Succeeds ~15s, up to 2 warnings OK. Warnings = errors via `Directory.Build.props` `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`.

**Important**: Run `dotnet workload restore` after clone or if WASM builds fail.

### Testing

**Run all tests**:
dotnet test
**Tests with coverage** (CI):
dotnet tool install --global dotnet-coverage
dotnet-coverage collect "dotnet test" -f xml -o "coverage.xml"
**Install Playwright browsers** (before UI tests):
# Install for all test projects
find . -type f -name 'playwright.ps1' | while read script; do
  pwsh "$script" install
done

# Or on Windows PowerShell:
Get-ChildItem -Recurse -Include "playwright.ps1" | ForEach-Object { pwsh $_.FullName install }
**Test Projects**: `*.UnitTests` suffix + integration tests:
- `PortfolioViewer.WASM.UITests` - Playwright UI tests (screenshots/videos on failure)
- `IntegrationTests` - General integration tests

### Running the Application

**Development (Aspire AppHost)**:
dotnet run --project PortfolioViewer/PortfolioViewer.AppHost/PortfolioViewer.AppHost.csproj
Launches Aspire dashboard + API service + Blazor WASM client.

**GhostfolioSidekick Console App**:
dotnet run --project GhostfolioSidekick/GhostfolioSidekick.csproj
**Docker Build** (see `Dockerfile`):
- Multi-stage build
- Installs Python, Node.js, wasm-tools, supervisord
- Builds PortfolioViewer.ApiService, PortfolioViewer.WASM, GhostfolioSidekick
- Platform-specific via `TARGETARCH`

## Project Layout & Architecture

### Repository Structure
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

### Key Architectural Patterns

1. **Blazor WebAssembly + .NET Aspire**: PortfolioViewer use Aspire for orchestration (`PortfolioViewer.AppHost`). WASM client ↔ `PortfolioViewer.ApiService` backend.

2. **EF Core + SQLite**: All DB ops via `Database/DatabaseContext`. Migrations in `Database/Migrations/` (excluded from SonarQube).

3. **Repository Pattern**: DB access via `Database.Repository` interfaces.

4. **Parser Strategy Pattern**: Broker/exchange parsers in `Parsers/` (e.g., Trading212, DeGiro, ScalableCapital).

5. **Config via JSON**: `Configuration/` project. JSON maps currencies, symbols, accounts.

### Important Configuration Files

- **`.editorconfig`**: Tabs (size 4), CRLF, UTF-8. Follow strictly.
- **`Directory.Build.props`**: `TreatWarningsAsErrors=true` globally - warnings block builds.
- **`GlobalSuppressions.cs`**: Code analysis suppressions (root).
- **`appsettings.json`**: Settings in `GhostfolioSidekick/` + `PortfolioViewer.ApiService/`.

### Database & Migrations

- **DB**: SQLite via EF Core 10.0
- **Migrations**: `Database/Migrations/`
- **Context**: `DatabaseContext` in `Database/`
- **Add Migration**: `dotnet ef migrations add <name> --project Database`
- **CRITICAL**: Always use `dotnet ef migrations add` for EF Core migrations. Never manually write migration files — the tooling generates correct snapshot diffs, rename operations, and Down methods. After adding, verify the generated migration matches the model state.
- **Update DB**: Apps handle migrations at startup

### Testing Infrastructure

- **Framework**: xUnit
- **UI Testing**: Playwright for `PortfolioViewer.WASM.UITests`
- **Playwright Setup**: `pwsh <test-project>/bin/Debug/net10.0/playwright.ps1 install` before first UI run
- **Mocking**: Moq + custom fixtures (`*UnitTests/`)
- **Test Output**: Screenshots (`playwright-screenshots/`) + videos (`playwright-videos/`) captured per test
  - Each test gets its own video directory: `playwright-videos/<TestName>/`
  - Videos recorded via `BrowserNewContextOptions.RecordVideoDir`
  - Error state capture: `CaptureErrorStateAsync()` saves screenshot + full HTML on failure
  - Browser console logs captured and printed: `[Browser Console] <type>: <text>`
  - Debug screenshots available in bin directory for failing tests

## CI/CD Pipeline (.github/workflows/docker-publish.yml)

Runs on: push to `master`, PRs to `master`, manual dispatch.

**Workflow Steps**:

1. **Checkout**: Full history (`fetch-depth: 0`)
2. **Setup**: JDK 21, .NET 10, Mono, WASM workload
3. **Install Playwright**: `npx playwright install` + per-project via `playwright.ps1`
4. **Install Tools**: `dotnet-sonarscanner`, `dotnet-coverage` (global)
5. **Build**: `dotnet build` (no errors)
6. **Test**: `dotnet-coverage collect "dotnet test" -f xml -o "coverage.xml"`
7. **SonarCloud**: Upload coverage + static analysis (needs `SONAR_TOKEN`)
8. **Artifacts**: Coverage, Playwright screenshots/videos
9. **Security Scans** (publish): Deprecated/vuln packages, OSV scanner

**CI Notes**:
- Tests run after Playwright install across all test projects
- Mono required on Linux for some builds
- WASM: `dotnet workload restore`
- SonarCloud: `VibeNL_GhostfolioSidekick` (org: `vibenl`)

## Coding Standards & Conventions

### Style

- **Indentation**: TABS (not spaces), width 4
- **Line Endings**: CRLF
- **Charset**: UTF-8
- **Naming**: PascalCase types/methods, interfaces start `I`
- **Nullability**: `<Nullable>enable</Nullable>`
- **Implicit Usings**: `<ImplicitUsings>enable</ImplicitUsings>`
- **Curly Brackets**: Always for single-line ifs

### Project Naming

Assemblies: `GhostfolioSidekick.<ProjectName>`
- Via `<AssemblyName>GhostfolioSidekick.$(MSBuildProjectName)</AssemblyName>`
- Namespace: `GhostfolioSidekick.<ProjectName.Replace(" ", "_")>`

### Code Quality

- **Production-Ready**: SOLID, DRY, YAGNI, best practices
- **Verify**: After every code change, run `dotnet build` (verify compilation) and `dotnet test` (verify all tests pass) before marking a task complete.
- **Warnings = Errors**: ALL warnings resolved before commit
- **SonarCloud**: Pass quality gate (README.md badge)
- **Exclusions**: JS + generated code excluded (`<SonarQubeSetting>` in `.csproj`)

## Common Workarounds & Gotchas

1. **WASM Build**: WASM errors → run `dotnet workload restore`
2. **Playwright**: UI test browser errors → `playwright.ps1 install` in test project bin
3. **DB Migrations**: Auto at startup. No manual `dotnet ef database update` unless dev migrations.
4. **Crypto**: `use.crypto.workaround.*` settings handle dust/staking edge cases (see README.md)
5. **Symbol Matching**: `GhostfolioAPI/GhostfolioSymbolMatcher.cs` has TODOs for improvements
6. **Line Endings**: CRLF. Git may convert. Respect `.gitattributes`.
7. **TypeScript**: e.g., `PortfolioViewer.WASM/wwwroot/js/site.ts` compiled via MSBuild + `Microsoft.TypeScript.MSBuild`

## Validation Checklist

Before submit:

1. ✅ `dotnet build` - 0 errors (2 warnings OK in full build)
2. ✅ `dotnet test` - all pass
3. ✅ `.editorconfig` - tabs, CRLF, UTF-8
4. ✅ WASM changes → `dotnet workload restore`
5. ✅ UI changes → consider Playwright tests
6. ✅ New deps → .NET 10.0 compatible
7. ✅ Code matches patterns (repo pattern, DI, etc.)
8. ✅ DB changes → add migrations, test apply/rollback

## Additional Resources

- **README.md**: Setup, config format, supported brokers
- **Documentation/Parsers/**: Parser docs
- **Examples/**: Config examples
- **GitHub Workflow**: `.github/workflows/docker-publish.yml` - definitive build/deploy
- **Dockerfile**: Prod deploy + supervisord for multi-process

---

**Trust these**: Search more only if incomplete or conflicts with repo state. Comprehensive + validated against repo + CI.
