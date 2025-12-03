using System.Text.RegularExpressions;

namespace GhostfolioSidekick.Utilities
{
    public static partial class SymbolNameCleaner
    {
        /// <summary>
        /// Cleans a symbol name by removing common suffixes and unwanted terms.
        /// </summary>
        /// <param name="name">The symbol name to clean.</param>
        /// <returns>The cleaned symbol name.</returns>
        public static string CleanSymbolName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return name;

            // Remove terms like 'co.', 'corp.', 'inc.', 'ltd.' (case-insensitive, with/without dot)
            var pattern = @"\b(co\.?|corp\.?|inc\.?|ltd\.?|plc|sa|nv|ag|group|holding|holdings|company|limited|incorporated|corporation)\b";
            var cleaned = Regex.Replace(name, pattern, string.Empty, RegexOptions.IgnoreCase);

            // Remove extra whitespace
            cleaned = MyRegex().Replace(cleaned, " ").Trim();

            // Remove trailing/leading punctuation
            cleaned = cleaned.Trim(',', '.', '-', '_');

            return cleaned;
        }

        [GeneratedRegex("\\s+")]
        private static partial Regex MyRegex();
    }
}
