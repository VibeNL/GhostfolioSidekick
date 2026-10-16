# Upgrade Packages Skill

Upgrade NuGet packages across the GhostfolioSidekick solution with safe, verified changes.

## What It Does

- Always starts on a fresh upgrade branch (never the default branch)
- Audits all ~45 projects for outdated NuGet packages
- Upgrades versions centrally in `Directory.Packages.props` (central package management), risk-tiered (patch → minor → major)
- Validates every change with build + tests
- Fixes compile issues caused by breaking changes
- Verifies no new security vulnerabilities
- Checks for deprecated packages

## Workflow

1. **Branch (mandatory)** — always start on a fresh branch from the default branch; never upgrade directly on it
2. **Audit** — `dotnet list package --outdated` per project (solution-level invocation fails on .NET 10)
3. **Upgrade** — bump `<PackageVersion>` entries in `Directory.Packages.props` (central package management), then `dotnet restore`
4. **Consolidate** — verify no per-project `Version=` overrides remain and every project resolves one version per package
5. **Build** — `dotnet build` (must be 0 errors, 0 warnings)
6. **Test** — `dotnet test` (must pass)
7. **Security** — `dotnet list package --vulnerable` + `--deprecated` per project (must be clean)
8. **Commit + Push** — commit with a descriptive message including package names, then `git push -u origin <branch>`

## Central Package Management

All package versions are pinned in `Directory.Packages.props` (`ManagePackageVersionsCentrally=true`). `.csproj` files hold version-less `<PackageReference>` entries only — upgrades happen by editing the props file, not per project. Under CPM, `dotnet add <project> package X --version Y` does not work.

## .NET 10 CLI Notes

- `dotnet list package ...` at the solution root fails (`error: Sequence contains no matching element`) — run it inside each project directory.
- Outdated output header is "has the following updates to its packages" with columns `Top-level Package | Requested | Resolved | Latest`; up-to-date projects print "has no updates given the current sources".

## Risk Tiers

| Tier | Type | Approach |
|------|------|----------|
| 1 | Patch/Minor | All at once |
| 2 | Major version | One at a time, verify between |
| 3 | .NET/EF Core | Manual review required |

## Trigger

Ask: "upgrade packages", "update dependencies", "nuget upgrade", or `/upgrade-packages`
