using System.Globalization;
using System.Text.RegularExpressions;

namespace GhostfolioSidekick.ExternalDataProvider.Citi
{
	/// <summary>
	/// Extracts the "Ratio (ORD:DRS)" field from the HTML/JS snippet returned by Citi's public
	/// Depositary Receipts "DR Program Information" widget
	/// (e.g. https://depositaryreceipts.citi.com/adr/guides/pgm_d.aspx?pageid=15&amp;subpageid=151&amp;cusip=&lt;cusip&gt;).
	/// Example fragment: "Ratio (ORD:DRS)&amp;nbsp;&lt;/td&gt;&lt;td class=\"mwRight\"&gt;25      :1       &lt;/td&gt;"
	/// </summary>
	public static partial class CitiAdrRatioParser
	{
		[GeneratedRegex(@"Ratio\s*\(ORD\s*:\s*DRS\)[^0-9]*(?<ord>\d+(\.\d+)?)\s*:\s*(?<drs>\d+(\.\d+)?)", RegexOptions.IgnoreCase)]
		private static partial Regex RatioPattern();

		/// <summary>
		/// Parses the shares-per-receipt ratio (ORD:DRS) from the given page content.
		/// For "Ratio (ORD:DRS) 25:1" this returns 25 (25 ordinary shares per 1 depositary receipt).
		/// Returns null when no ratio could be found or the DRS side is not 1.
		/// </summary>
		public static decimal? TryParseSharesPerReceipt(string? pageContent)
		{
			if (string.IsNullOrWhiteSpace(pageContent))
			{
				return null;
			}

			var match = RatioPattern().Match(pageContent);
			if (!match.Success)
			{
				return null;
			}

			if (!decimal.TryParse(match.Groups["ord"].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var ord) ||
				!decimal.TryParse(match.Groups["drs"].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var drs) ||
				drs <= 0)
			{
				return null;
			}

			return ord / drs;
		}

		/// <summary>
		/// Derives the CUSIP from a US-issued ISIN (US + 9 character CUSIP + check digit).
		/// Returns null for non-US ISINs or invalid input.
		/// </summary>
		public static string? TryGetCusipFromIsin(string? isin)
		{
			if (string.IsNullOrWhiteSpace(isin) || isin.Length != 12)
			{
				return null;
			}

			if (!isin.StartsWith("US", StringComparison.OrdinalIgnoreCase))
			{
				return null;
			}

			return isin.Substring(2, 9);
		}
	}
}
