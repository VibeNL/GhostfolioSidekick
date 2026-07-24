using System.Globalization;
using System.Text.RegularExpressions;

namespace GhostfolioSidekick.ExternalDataProvider.Yahoo
{
	/// <summary>
	/// Best-effort extraction of the ADR (American Depositary Receipt) / GDR (Global Depositary Receipt)
	/// "shares per receipt" ratio from free-text security descriptions (e.g. instrument name or business summary).
	/// There is no dedicated free API that exposes this ratio directly, so this heuristically parses
	/// commonly used phrasings such as:
	///   "GDR (EACH REP 25 COM STK KRW100)"
	///   "each ADR represents 4 ordinary shares"
	///   "one ADS represents 0.5 of one ordinary share"
	///   "ADR ratio of 10:1"
	/// Returns null when no ratio could be determined; callers should default to 1 (no conversion) in that case.
	/// </summary>
	public static partial class AdrRatioParser
	{
		private static readonly Regex[] Patterns =
		[
			// "EACH REP 25", "EACH REPRESENTING 25", "EACH REPRESENTS 25"
			new(@"EACH\s+REP(?:RESENTING|RESENTS)?\.?\s+(?<ratio>\d+(\.\d+)?)", RegexOptions.IgnoreCase | RegexOptions.Compiled),

			// "one ADR represents 4 ordinary shares", "each ADS represents 0.5 of one ordinary share"
			new(@"(?:one|each|1)\s+(?:ADR|ADS|GDR|GDS|DR)s?\s+represents?\s+(?<ratio>\d+(\.\d+)?)", RegexOptions.IgnoreCase | RegexOptions.Compiled),

			// "ADR ratio of 10:1", "ADR ratio: 10:1", "GDR ratio 4:1"
			new(@"(?:ADR|ADS|GDR|GDS|DR)\s+ratio\s*(?:of|is|:)?\s*(?<ratio>\d+(\.\d+)?)\s*:\s*1", RegexOptions.IgnoreCase | RegexOptions.Compiled),

			// "represents 25 ordinary shares" / "represents 25 common shares"
			new(@"represents?\s+(?<ratio>\d+(\.\d+)?)\s+(?:ordinary|common)\s+shares?", RegexOptions.IgnoreCase | RegexOptions.Compiled),
		];

		/// <summary>
		/// Attempts to determine the number of underlying ordinary shares represented by one ADR/GDR unit
		/// by scanning the provided text fragments (e.g. security long name, short name, business summary).
		/// </summary>
		public static decimal? TryParseSharesPerReceipt(params string?[] textFragments)
		{
			foreach (var text in textFragments)
			{
				if (string.IsNullOrWhiteSpace(text))
				{
					continue;
				}

				foreach (var pattern in Patterns)
				{
					var match = pattern.Match(text);
					if (!match.Success)
					{
						continue;
					}

					if (decimal.TryParse(match.Groups["ratio"].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var ratio) && ratio > 0)
					{
						return ratio;
					}
				}
			}

			return null;
		}
	}
}
