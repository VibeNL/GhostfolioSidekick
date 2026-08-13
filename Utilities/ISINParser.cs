namespace GhostfolioSidekick.Utilities
{
	public static class ISINParser
	{
		public static string ExtractIsin(string line)
		{
			if (string.IsNullOrWhiteSpace(line))
			{
				return string.Empty;
			}

			if (line.StartsWith("ISIN:", StringComparison.InvariantCultureIgnoreCase))
			{
				var value = line.Substring("ISIN:".Length).Trim();
				return IsIsin(value) ? value : string.Empty;
			}

			return IsIsin(line.Trim()) ? line.Trim() : string.Empty;
		}

		public static string ExtractIsinMultistring(string descriptionString)
		{
			if (string.IsNullOrWhiteSpace(descriptionString))
			{
				return string.Empty;
			}

			var lines = descriptionString.Split([' '], StringSplitOptions.RemoveEmptyEntries);
			foreach (var line in lines)
			{
				var isin = ExtractIsin(line);
				if (!string.IsNullOrEmpty(isin))
				{
					return isin;
				}
			}

			return string.Empty;
		}

		/// <summary>
		/// Returns the ISIN if it is a valid ISIN format, otherwise null.
		/// </summary>
		public static string? GetValidIsin(string? isin)
		{
			return !string.IsNullOrEmpty(isin) && IsIsin(isin) ? isin : null;
		}

		public static bool IsIsin(string line)
		{
			if (line.Length != 12)
			{
				return false;
			}

			// ISIN format: 2 letters, 9 alphanumeric characters, 1 digit
			if (!char.IsLetter(line[0]) || !char.IsLetter(line[1]))
			{
				return false;
			}

			for (int i = 2; i < 11; i++)
			{
				if (!char.IsLetterOrDigit(line[i]))
				{
					return false;
				}
			}

			if (!char.IsDigit(line[11]))
			{
				return false;
			}

			return true;
		}
	}
}