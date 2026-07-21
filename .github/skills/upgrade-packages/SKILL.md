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

### Phase 2b: Consolidate Versions

**Always consolidate — no package should have multiple versions across projects.**

5. Check for duplicate package versions:
   ```
   dotnet list package --vulnerable --include-transitive
   ```
   Also grep all .csproj files for each upgraded package to verify uniform versions:
   ```
   grep -r "PackageReference Include=\"<PackageName>\"" --include="*.csproj"
   ```
   Or on Windows:
   ```
   findstr /s /i "PackageReference Include=\"<PackageName>\"" *.csproj
   ```

6. Align all projects to the same version:
   - Upgrade any project still on an older version
   - If a project intentionally needs a different version, justify and document it
   - Never leave the same package at multiple versions unless there is a hard compatibility reason

7. Rebuild after consolidation to catch version conflicts:
   ```
   dotnet build
   ```

### Phase 3: Validate Build

8. Build entire solution:
   ```
   dotnet build
   ```
   - Must succeed with 0 errors
   - TreatWarningsAsErrors=true — fix ALL new warnings
   - If warnings appear, fix them immediately before proceeding

9. Fix compile issues:
   - API changes from package upgrades (removed methods, renamed types, changed signatures)
   - Breaking changes in dependency contracts
   - Nullable reference type issues
   - DO NOT suppress warnings — fix the root cause

### Phase 4: Validate Tests

10. Run all tests:
   ```
   dotnet test
   ```
   - All tests must pass
   - If UI tests fail, check Playwright browser compatibility
   - Fix test failures caused by package changes

11. Run with coverage (if available):
   ```
   dotnet-coverage collect "dotnet test" -f xml -o "coverage.xml"
   ```

### Phase 5: Security Verification

12. Scan for vulnerable packages:
   ```
   dotnet list package --vulnerable --include-transitive
   ```
   - Must show no vulnerable packages
   - If vulnerabilities found, upgrade those packages immediately

13. Check for deprecated packages:
    ```
    dotnet list package --deprecated
    ```
    - Upgrade or replace deprecated packages

### Phase 6: Commit

14. Commit changes with a descriptive message:
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
3. ✅ All upgraded packages at single version across all projects (no duplicates)
4. ✅ `dotnet list package --vulnerable` — no vulnerabilities
5. ✅ `dotnet list package --deprecated` — no deprecated packages
6. ✅ No `.editorconfig` violations (tabs, CRLF, UTF-8)
7. ✅ .NET 10.0 compatibility maintained

## Boundaries

- This skill upgrades NuGet packages only (not OS packages, Node.js, or Playwright)
- Does not modify application logic beyond what's required for compatibility
- Does not approve PRs or create branches — output changes ready for review
- If a major version upgrade would require extensive refactoring, flag it and recommend deferring
