# .NET 10 Upgrade Plan

## Table of Contents
- [1. Executive Summary](#1-executive-summary)
- [2. Migration Strategy](#2-migration-strategy)
- [3. Detailed Dependency Analysis](#3-detailed-dependency-analysis)
- [4. Project-by-Project Plans](#4-project-by-project-plans)
- [5. Risk Management](#5-risk-management)
- [6. Testing & Validation Strategy](#6-testing--validation-strategy)
- [7. Complexity & Effort Assessment](#7-complexity--effort-assessment)
- [8. Source Control Strategy](#8-source-control-strategy)
- [9. Success Criteria](#9-success-criteria)

---

## 1. Executive Summary

### Scenario Description
Upgrade all projects in the GhostfolioSidekick solution from .NET 9.0 and .NET Framework 4.8.1 to .NET 10.0 (LTS).

### Scope
- **Total Projects:** 45 (all require upgrade)
- **Project Types:** Blazor, WebAssembly, ASP.NET Core, Class Libraries, Test Projects
- **Current Frameworks:** net9.0, net481
- **Target Framework:** net10.0 (LTS)
- **Total NuGet Packages:** 84 (21 need upgrade)
- **Total Lines of Code:** ~81,000
- **Estimated LOC to modify:** 281+ (at least 0.3% of codebase)
- **API Issues:** 384 (binary/source/behavioral incompatibilities)
- **Blazor/WebAssembly:** Prioritized for compatibility and package updates

### Selected Strategy
**All-At-Once Strategy** — All projects upgraded simultaneously in a single atomic operation.

**Rationale:**
- Solution is medium-sized (45 projects, but most are SDK-style and low complexity)
- Dependency graph is well-defined, no deep cycles
- Most projects have low estimated LOC impact
- All package updates and API changes are known and catalogued
- Good test coverage across solution

### Complexity Assessment
- **Classification:** Medium complexity (many projects, but low per-project impact)
- **Critical Issues:** 1 incompatible NuGet package, several behavioral/source API changes, Blazor/WebAssembly compatibility
- **Iteration Strategy:** 2-3 detail iterations (atomic upgrade, validation, risk mitigation)

## 2. Migration Strategy
[To be filled]

## 3. Detailed Dependency Analysis
[To be filled]

## 4. Project-by-Project Plans
[To be filled]

## 5. Risk Management
[To be filled]

## 6. Testing & Validation Strategy
[To be filled]

## 7. Complexity & Effort Assessment
[To be filled]

## 8. Source Control Strategy
[To be filled]

## 9. Success Criteria
[To be filled]
