# Upgrade Packages Skill

Upgrade NuGet packages across the GhostfolioSidekick solution with safe, verified changes.

## What It Does

- Audits all ~45 projects for outdated NuGet packages
- Upgrades packages with risk-tiered approach (patch → minor → major)
- Validates every change with build + tests
- Fixes compile issues caused by breaking changes
- Verifies no new security vulnerabilities
- Checks for deprecated packages

## Workflow

1. **Audit** — `dotnet list package --outdated --include-transitive`
2. **Upgrade** — `dotnet add <project> package <name> --version <ver>`
3. **Build** — `dotnet build` (must be 0 errors, 0 warnings)
4. **Test** — `dotnet test` (must pass)
5. **Security** — `dotnet list package --vulnerable` (must be clean)
6. **Commit** — descriptive message with package names

## Risk Tiers

| Tier | Type | Approach |
|------|------|----------|
| 1 | Patch/Minor | All at once |
| 2 | Major version | One at a time, verify between |
| 3 | .NET/EF Core | Manual review required |

## Trigger

Ask: "upgrade packages", "update dependencies", "nuget upgrade", or `/upgrade-packages`
