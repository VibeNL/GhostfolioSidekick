namespace GhostfolioSidekick.Utilities
{
	public static class ListExtensions
	{
		public static List<T> FilterEmpty<T>(this IEnumerable<T?> list) where T : class
		{
			return [.. list.Where(item => item != null).Select(item => item!)];
		}

		public static List<string> FilterInvalidNames(this IEnumerable<string?> list)
		{
			var invalidNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{
				"Inc", "Inc.", "Corp", "Corp.", "Corporation", "Ltd", "Ltd.",
				"Limited", "LLC", "L.L.C.", "LP", "L.P.", "LLP", "L.L.P.",
				"Co", "Co.", "Company", "Group", "Holdings", "Holding",
				"Trust", "Fund", "Partners", "Partnership", "PLC", "P.L.C.",
				"SA", "S.A.", "AG", "GmbH", "SE", "NV", "N.V.", "BV", "B.V.",
				"Plc", "The", "A", "An", "And", "&", "Of", "For", "In", "On",
				"At", "To", "From", "With", "By", "As", "Is", "Was", "Are",
				"Were", "Be", "Been", "Being", "Have", "Has", "Had", "Do",
				"Does", "Did", "Will", "Would", "Could", "Should", "May",
				"Might", "Must", "Can", "Cannot"
			};

			return [.. list
				.Where(item => !string.IsNullOrWhiteSpace(item))
				.Where(item => !invalidNames.Contains(item!.Trim()))
				.Where(item => item!.Trim().Length > 1) // Filter out single characters
				.Select(item => item!.Trim())
				.Distinct()];
		}

		/// <summary>
		/// Trims .DE and .AS suffixes from symbol strings and returns distinct cleaned symbols.
		/// </summary>
		public static List<string> TrimSymbolSuffixes(this IEnumerable<string?> list)
		{
         return [.. list
				.Where(item => !string.IsNullOrWhiteSpace(item))
				.Select(item =>
				{
					var trimmed = item!.Trim();
					if (trimmed.Length > 3 && trimmed[^3] == '.' && char.IsLetter(trimmed[^2]) && char.IsLetter(trimmed[^1]))
						trimmed = trimmed[..^3];
					return trimmed;
				})
				.Where(item => item.Length > 0)
				.Distinct(StringComparer.OrdinalIgnoreCase)
			];
		}
	}
}
