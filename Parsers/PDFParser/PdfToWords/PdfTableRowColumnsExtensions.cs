namespace GhostfolioSidekick.Parsers.PDFParser.PdfToWords
{
	public static class PdfTableRowColumnsExtensions
	{
		/// <summary>
		/// Gets the column value by matching the column name from the header keywords.
		/// This method assumes that the PdfTableRowColumns was created using FindTableRowsWithColumns
		/// with a specific headerKeywords array, and the columns are aligned in the same order.
		/// </summary>
		/// <param name="rowColumns">The row with columns</param>
		/// <param name="header">The header row containing column names</param>
		/// <param name="columnName">The name of the column to retrieve (should match one of the header keywords)</param>
		/// <returns>The text value of the column, or null if not found</returns>
		public static string? GetColumnValue(this PdfTableRowColumns? rowColumns, PdfTableRow? header, string? columnName)
		{
			if (rowColumns?.Columns == null || header?.Tokens == null)
			{
				return null;
			}

			var reconstructedKeywords = ExtractHeaderKeywords(header);
			var columnIndex = FindColumnIndex(reconstructedKeywords, columnName);

			if (columnIndex < 0 || columnIndex >= rowColumns.Columns.Count)
			{
				return null;
			}

			var columnTokens = rowColumns.Columns[columnIndex];
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

		private static string[] ExtractHeaderKeywords(PdfTableRow header)
		{
			if (header?.Tokens == null)
			{
				return [];
			}

			// Group tokens by their column positions to reconstruct the original keywords
			var tokenGroups = header.Tokens
				.Where(t => t.BoundingBox != null)
				.GroupBy(t => t.BoundingBox!.Column, (col, tokens) => tokens.ToList())
				.OrderBy(group => group.First().BoundingBox!.Column)
				.ToList();

			// If tokens are too close together (same column), merge them
			var mergedGroups = new List<List<SingleWordToken>>();
			foreach (var group in tokenGroups)
			{
				if (mergedGroups.Count == 0)
				{
					mergedGroups.Add(group);
				}
				else
				{
					var lastGroup = mergedGroups[^1];
					var lastColumn = lastGroup.Last().BoundingBox!.Column;
					var currentColumn = group.First().BoundingBox!.Column;

					// If columns are close (within 5 units), they're part of the same keyword
					if (Math.Abs(currentColumn - lastColumn) <= 5)
					{
						lastGroup.AddRange(group);
					}
					else
					{
						mergedGroups.Add(group);
					}
				}
			}

			// Convert each group to a keyword string
			return [.. mergedGroups.Select(group => string.Join(" ", group.OrderBy(t => t.BoundingBox!.Column).Select(t => t.Text)))];
		}
	}
}
