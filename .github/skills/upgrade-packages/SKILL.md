---
name: upgrade-packages
description: >
  Upgrade NuGet packages across the GhostfolioSidekick .NET 10 solution.
  Finds outdated packages, upgrades them, always validates with build + tests,
  fixes compile issues, and verifies no security vulnerabilities.
  Trigger: "upgrade packages", "update dependencies", "nuget upgrade",
  "bump versions", "update all packages", /upgrade-packages.
---

Upgrade NuGet packages across the GhostfolioSidekick solution safely.

## Scope

- ~45 .csproj files in the solution
- .NET 10.0 (preview) — must remain compatible
- TreatWarningsAsErrors=true globally — no new warnings allowed
- SonarCloud quality gate must pass

## Trigger

Use when user asks any of:
- "upgrade packages", "update dependencies", "nuget upgrade"
- "bump versions", "update all packages", "update NuGet packages"
- invokes /upgrade-packages

## Runbook

### Phase 1: Audit

1. List outdated packages across all projects:
   ```
   dotnet list package --outdated --include-transitive
   ```
   Run per project or at solution root. Capture all results.

2. Identify breaking-change risks:
   - Major version bumps are high risk
   - Note .NET runtime/SDK version constraints
   - Flag packages with known vulnerabilities

### Phase 2: Upgrade

3. Upgrade packages per project (group by risk):
   ```
   dotnet add <project>.csproj package <PackageName> --version <newVersion>
   ```
   Or for all in a project:
   ```
   dotnet add <project>.csproj package <PackageName> --prerelease
   ```
   - Upgrade minor/patch versions first (lower risk)
   - Upgrade major versions one at a time, verify between each
   - Keep .NET runtime/SDK packages aligned

4. Restore and resolve:
   ```
   dotnet restore
   dotnet workload restore
   ```

### Phase 3: Validate Build

5. Build entire solution:
   ```
   dotnet build
   ```
   - Must succeed with 0 errors
   - TreatWarningsAsErrors=true — fix ALL new warnings
   - If warnings appear, fix them immediately before proceeding

6. Fix compile issues:
   - API changes from package upgrades (removed methods, renamed types, changed signatures)
   - Breaking changes in dependency contracts
   - Nullable reference type issues
   - DO NOT suppress warnings — fix the root cause

### Phase 4: Validate Tests

7. Run all tests:
   ```
   dotnet test
   ```
   - All tests must pass
   - If UI tests fail, check Playwright browser compatibility
   - Fix test failures caused by package changes

8. Run with coverage (if available):
   ```
   dotnet-coverage collect "dotnet test" -f xml -o "coverage.xml"
   ```

### Phase 5: Security Verification

9. Scan for vulnerable packages:
   ```
   dotnet list package --vulnerable --include-transitive
   ```
   - Must show no vulnerable packages
   - If vulnerabilities found, upgrade those packages immediately

10. Check for deprecated packages:
    ```
    dotnet list package --deprecated
    ```
    - Upgrade or replace deprecated packages

### Phase 6: Commit

11. Commit changes with a descriptive message:
    ```
    chore(deps): upgrade NuGet packages
    ```
    - Include specific package names and version ranges in body
    - Note any breaking changes addressed

## Risk Tiers

**Tier 1 — Safe (upgrade all at once):**
- Patch versions (x.y.Z)
- Minor versions (x.Y.z) when no major versions affected
- Direct dependencies with wide compatibility

**Tier 2 — Careful (upgrade one at a time, verify between each):**
- Major version bumps (X.y.z)
- Packages with tight coupling across multiple projects
- Runtime/SDK packages

**Tier 3 — Manual review required:**
- .NET runtime/SDK upgrades
- EF Core major version changes
- Framework packages with API surface changes

## Validation Checklist

Before marking upgrade complete:
1. ✅ `dotnet build` — 0 errors, 0 warnings
2. ✅ `dotnet test` — all pass
3. ✅ `dotnet list package --vulnerable` — no vulnerabilities
4. ✅ `dotnet list package --deprecated` — no deprecated packages
5. ✅ No `.editorconfig` violations (tabs, CRLF, UTF-8)
6. ✅ .NET 10.0 compatibility maintained

## Boundaries

- This skill upgrades NuGet packages only (not OS packages, Node.js, or Playwright)
- Does not modify application logic beyond what's required for compatibility
- Does not approve PRs or create branches — output changes ready for review
- If a major version upgrade would require extensive refactoring, flag it and recommend deferring
