// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

// Performance suppressions
[assembly: SuppressMessage("Performance", "CA1873:Avoid potentially expensive logging")]
[assembly: SuppressMessage("Performance", "CA1861:Avoid constant arrays as arguments")]

// Usage suppressions
[assembly: SuppressMessage("Usage", "CA1816:Dispose methods should call SuppressFinalize")]
[assembly: SuppressMessage("Usage", "CA2254:Template should be a static expression")]

// Reliability suppressions
[assembly: SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly")]

// Major Code Smell suppressions
[assembly: SuppressMessage("Major Code Smell", "S3881:\"IDisposable\" should be implemented correctly")]
[assembly: SuppressMessage("Major Code Smell", "S2629:Logging templates should be constant", Justification = "TODO")]
[assembly: SuppressMessage("Major Code Smell", "S3925:\"ISerializable\" should be implemented correctly")]
[assembly: SuppressMessage("Major Code Smell", "S2234:Arguments should be passed in the same order as the method parameters", Justification = "False positive")]
[assembly: SuppressMessage("Major Code Smell", "S107:Methods should not have too many parameters", Justification = "DDD")]

// Critical Code Smell suppressions
[assembly: SuppressMessage("Critical Code Smell", "S3776:Cognitive Complexity of methods should not be too high")]

// Minor Code Smell suppressions
[assembly: SuppressMessage("Minor Code Smell", "S6608:Prefer indexing instead of \"Enumerable\" methods on types implementing \"IList\"")]
[assembly: SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded")]
[assembly: SuppressMessage("Minor Code Smell", "S6664:The code block contains too many logging calls")]
[assembly: SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Known abbreviation")]
[assembly: SuppressMessage("Minor Code Smell", "S1192:String literals should not be duplicated", Justification = "API definition")]
[assembly: SuppressMessage("Minor Code Smell", "S3267:Loops should be simplified with \"LINQ\" expressions")]

// Info Code Smell suppressions
[assembly: SuppressMessage("Info Code Smell", "S1135:Track uses of \"TODO\" tags")]

// Maintainability suppressions
[assembly: SuppressMessage("Maintainability", "S2139:Either log this exception and handle it, or rethrow it with some contextual information.")]


// Code Quality suppressions
[assembly: SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "<Pending>", Scope = "type", Target = "~T:GhostfolioSidekick.ExternalDataProvider.DividendMax.DividendMaxMatcher.SuggestResult")]
