---
name: upgrade-packages
description: >
  Upgrade NuGet packages across the GhostfolioSidekick .NET 10 solution using
  central package management (Directory.Packages.props). Always starts on a new
  branch, finds outdated packages, upgrades them, always validates with build +
  tests, fixes compile issues, and verifies no security vulnerabilities.
  Trigger: "upgrade packages", "update dependencies", "nuget upgrade",
  "bump versions", "update all packages", /upgrade-packages.
---

Upgrade NuGet packages across the GhostfolioSidekick solution safely.

## Scope

- ~45 .csproj files in the solution
- .NET 10.0 (preview) — must remain compatible
- **Central package management**: all versions pinned in `Directory.Packages.props` (`ManagePackageVersionsCentrally=true`). `.csproj` files contain version-less `<PackageReference>` entries only.
- TreatWarningsAsErrors=true globally — no new warnings allowed
- SonarCloud quality gate must pass

## Trigger

Use when user asks any of:
- "upgrade packages", "update dependencies", "nuget upgrade"
- "bump versions", "update all packages", "update NuGet packages"
- invokes /upgrade-packages

## Runbook

### Phase 0: Branch (mandatory, before any change)

A new branch is ALWAYS required — never commit upgrades directly to the default branch.

1. Start from a clean default branch:
   ```
   git checkout main && git pull
   ```
2. Create a fresh upgrade branch:
   ```
   git checkout -b package-upgrades-YYYYMMDD
   ```
   If already on an existing upgrade branch with uncommitted work, continue there instead of creating another.

### Phase 1: Audit (.NET 10 specifics)

.NET 10 CLI behavior — required to know:
- `dotnet list package --outdated` at the **solution root fails** (`error: Sequence contains no matching element`). It must be run **per project**: cd into each project directory first.
- Output format differs from older SDKs. Per-project output is either:
  - `The given project \`X\` has no updates given the current sources.` — up to date, or
  - `Project \`X\` has the following updates to its packages` followed by a table with columns `Top-level Package | Requested | Resolved | Latest`; rows for direct dependencies are prefixed `> `.

1. List outdated packages per project:
   ```
   dotnet list package --outdated --include-transitive
   ```
   Run in every project directory (~45 projects). Capture all results.

2. Identify breaking-change risks:
   - Major version bumps are high risk
   - Note .NET runtime/SDK version constraints
   - Flag packages with known vulnerabilities

**Required output:** per-project outdated table (Package / Requested / Resolved / Latest) for every project, plus the risk-tier grouping from below.

### Phase 2: Upgrade (via Central Package Management)

3. Bump versions in `Directory.Packages.props` — the single source of truth for all package versions:
   ```xml
   <PackageVersion Include="PackageName" Version="<newVersion>" />
   ```
   - Under CPM, `dotnet add <project>.csproj package X --version Y` does NOT work (versions are centralized) — edit `Directory.Packages.props` directly.
   - Upgrade minor/patch versions first (lower risk)
   - Upgrade major versions one at a time, verify between each
   - Keep .NET runtime/SDK packages aligned

4. Restore and resolve:
   ```
   dotnet restore
   dotnet workload restore
   ```

**Required output:** `git diff Directory.Packages.props` showing old → new version for every bumped package.

### Phase 2b: Consolidate Versions

With CPM, versions are single-sourced by design — no package can have multiple versions across projects unless a `.csproj` overrides it. Still verify:

5. No per-project version overrides remain in any `.csproj`:
   ```
   grep -r "PackageReference[^>]*Version=" --include="*.csproj" .
   ```
   Must return nothing. If a project intentionally needs a different version, justify and document it — never leave an unexplained override.

6. Confirm every project resolves the upgraded package to the same version:
   ```
   dotnet list <project>.csproj package
   ```
   Check a sample of projects (or resolved versions in `obj/project.assets.json`).

7. Rebuild after consolidation to catch version conflicts:
   ```
   dotnet build
   ```

**Required output:** empty grep result + one resolved version per upgraded package across all projects.

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

**Required output:** full `dotnet build` log showing 0 errors and 0 warnings.

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

**Required output:** `dotnet test` summary line(s) — total / passed / failed counts, all green.

### Phase 5: Security Verification

Run per project (like Phase 1) — solution-level `dotnet list package` fails on .NET 10.

12. Scan for vulnerable packages:
   ```
   dotnet list package --vulnerable --include-transitive
   ```
    - Must show no vulnerable packages
    - If vulnerabilities found, upgrade those packages immediately (back in Phase 2)

13. Check for deprecated packages:
    ```
   dotnet list package --deprecated
   ```
    - Upgrade or replace deprecated packages

**Required output:** both scans clean — no vulnerable and no deprecated entries across all projects.

### Phase 6: Commit & Push

The dedicated branch was created in Phase 0 — never commit upgrades directly to the default branch.

14. Commit changes with a descriptive message:
    ```
    chore(deps): upgrade NuGet packages
    ```
    - Include specific package names and version ranges in body
    - Note any breaking changes addressed

15. Push the branch to origin:
    ```
    git push -u origin <branch>
    ```

**Required output:** commit hash + successful `git push` (remote tracking set).

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
1. ✅ Work done on a dedicated upgrade branch created in Phase 0 — never directly on the default branch
2. ✅ `dotnet build` — 0 errors, 0 warnings
3. ✅ `dotnet test` — all pass
4. ✅ All versions pinned only in `Directory.Packages.props`; no per-project `Version=` attributes in any `.csproj`
5. ✅ Every upgraded package resolves to a single version across all projects (no duplicates)
6. ✅ `dotnet list package --vulnerable` — no vulnerabilities
7. ✅ `dotnet list package --deprecated` — no deprecated packages
8. ✅ No `.editorconfig` violations (tabs, CRLF, UTF-8)
9. ✅ .NET 10.0 compatibility maintained

## Boundaries

- This skill upgrades NuGet packages only (not OS packages, Node.js, or Playwright)
- Does not modify application logic beyond what's required for compatibility
- Always works on a dedicated upgrade branch created in Phase 0 (never the default branch); pushes it but does not open or approve PRs — the pushed branch is the review-ready output
- If a major version upgrade would require extensive refactoring, flag it and recommend deferring
