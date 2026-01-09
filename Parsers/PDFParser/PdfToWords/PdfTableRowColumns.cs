namespace GhostfolioSidekick.Parsers.PDFParser.PdfToWords
{
	public sealed record PdfTableRowColumns(string[] Headers, int Page, int Row, IReadOnlyList<IReadOnlyList<SingleWordToken>> Columns)
	{
		public string Text => string.Join(" ", Columns.SelectMany(c => c).Select(t => t.Text));

		/// <summary>
		/// Gets the column value by matching the column name from the headers.
		/// </summary>
		/// <param name="columnName">The name of the column to retrieve</param>
		/// <returns>The text value of the column, or null if not found</returns>
		public string? GetColumnValue(string? columnName)
		{
			if (string.IsNullOrWhiteSpace(columnName) || Headers == null || Columns == null)
			{
				return null;
			}

			var columnIndex = FindColumnIndex(Headers, columnName);

			if (columnIndex < 0 || columnIndex >= Columns.Count)
			{
				return null;
			}

			var columnTokens = Columns[columnIndex];
			if (columnTokens?.Count > 0)
			{
				return string.Join(" ", columnTokens.Select(t => t.Text));
			}

			return null;
		}

		private static int FindColumnIndex(string[] headerKeywords, string? columnName)
		{
			if (string.IsNullOrWhiteSpace(columnName) || headerKeywords == null)
			{
				return -1;
			}

			// Look for an exact match first (case-insensitive)
			for (int i = 0; i < headerKeywords.Length; i++)
			{
				if (string.Equals(headerKeywords[i], columnName, StringComparison.InvariantCultureIgnoreCase))
				{
					return i;
				}
			}

			// If no exact match, try to find a keyword that contains all parts of the column name
			var columnParts = columnName.Split(' ', StringSplitOptions.RemoveEmptyEntries);

			for (int i = 0; i < headerKeywords.Length; i++)
			{
				var keyword = headerKeywords[i];
				var keywordParts = keyword.Split(' ', StringSplitOptions.RemoveEmptyEntries);

				// Check if the keyword contains all parts of the column name
				if (columnParts.All(part => keywordParts.Any(kp => string.Equals(kp, part, StringComparison.InvariantCultureIgnoreCase))))
				{
					return i;
				}
			}

			return -1;
		}

		internal bool HasHeader(string[] header)
		{
			return
				header.Length == Headers.Length &&
				header.All(h =>
					!string.IsNullOrWhiteSpace(h) &&
					Headers.Any(rh => string.Equals(rh, h, StringComparison.InvariantCultureIgnoreCase)));

		}
	}
}
