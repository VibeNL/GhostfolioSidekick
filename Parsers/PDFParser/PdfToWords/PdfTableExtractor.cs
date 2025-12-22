using System.Globalization;

namespace GhostfolioSidekick.Parsers.PDFParser.PdfToWords
{
	public sealed record PdfTableRow(int Page, int Row, IReadOnlyList<SingleWordToken> Tokens)
	{
		public string Text => string.Join(" ", Tokens.Select(t => t.Text));
	}

	public sealed record PdfTableRowColumns(int Page, int Row, IReadOnlyList<IReadOnlyList<SingleWordToken>> Columns)
	{
		public string Text => string.Join(" ", Columns.SelectMany(c => c).Select(t => t.Text));
	}

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

			// Extract the original keywords that would have been used to create the columns
			// This simulates what the original headerKeywords array would have looked like
			var reconstructedKeywords = ExtractHeaderKeywords(header);

			// Find the index of the requested column name in the reconstructed keywords
			var columnIndex = FindColumnIndex(reconstructedKeywords, columnName);

			if (columnIndex < 0 || columnIndex >= rowColumns.Columns.Count)
			{
				return null;
			}

			// Get the tokens for this column and join them into a text value
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
				return Array.Empty<string>();
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
			return mergedGroups
				.Select(group => string.Join(" ", group.OrderBy(t => t.BoundingBox!.Column).Select(t => t.Text)))
				.ToArray();
		}
	}

	public static class PdfTableExtractor
	{
		public static List<PdfTableRow> GroupRows(IEnumerable<SingleWordToken> words)
		{
			return [.. words
				.Where(w => w.BoundingBox != null)
				.GroupBy(w => new { w.BoundingBox!.Page, w.BoundingBox!.Row }) // group by page + y-position (row)
				.OrderBy(g => g.Key.Page)
				.ThenBy(g => g.Key.Row)
				.Select(g => new PdfTableRow(
					g.Key.Page,
					g.Key.Row,
					g.OrderBy(w => w.BoundingBox!.Column).ToList()))
				];
		}

		public static List<PdfTableRow> FindTableRows(
			IEnumerable<SingleWordToken> words,
			string[] headerKeywords,
			Func<PdfTableRow, bool>? stopPredicate = null,
			Func<PdfTableRow, PdfTableRow, bool>? mergePredicate = null)
		{
			var rows = GroupRows(words);
			if (rows.Count == 0)
			{
				return [];
			}

			int headerIndex = rows.FindIndex(r => RowContainsAll(r, headerKeywords));
			if (headerIndex == -1)
			{
				return [];
			}

			var page = rows[headerIndex].Page;
			var result = new List<PdfTableRow>();
			for (int i = headerIndex + 1; i < rows.Count; i++)
			{
				var row = rows[i];
				if (row.Page != page)
				{
					break;
				}

				if (stopPredicate != null && stopPredicate(row))
				{
					break;
				}

				result.Add(row);
			}

			return mergePredicate == null ? result : MergeMultilineRows(result, mergePredicate);
		}

		public static (PdfTableRow Header, List<PdfTableRowColumns> Rows) FindTableRowsWithColumns(
			IEnumerable<SingleWordToken> words,
			string[] headerKeywords,
			Func<PdfTableRow, bool>? stopPredicate = null,
			Func<PdfTableRow, PdfTableRow, bool>? mergePredicate = null)
		{
			return FindTableRowsWithColumns(words, headerKeywords, null, stopPredicate, mergePredicate);
		}

		public static (PdfTableRow Header, List<PdfTableRowColumns> Rows) FindTableRowsWithColumns(
			IEnumerable<SingleWordToken> words,
			string[] headerKeywords,
			IReadOnlyList<ColumnAlignmentConfig>? alignmentConfigs,
			Func<PdfTableRow, bool>? stopPredicate = null,
			Func<PdfTableRow, PdfTableRow, bool>? mergePredicate = null)
		{
			var rows = GroupRows(words);
			if (rows.Count == 0)
			{
				return (new PdfTableRow(0, 0, Array.Empty<SingleWordToken>()), []);
			}

			int headerIndex = rows.FindIndex(r => RowContainsAll(r, headerKeywords));
			if (headerIndex == -1)
			{
				return (new PdfTableRow(0, 0, Array.Empty<SingleWordToken>()), []);
			}

			var header = GetHeaders(headerKeywords, rows[headerIndex]);

			var dataRows = new List<PdfTableRow>();
			for (int i = headerIndex + 1; i < rows.Count; i++)
			{
				var row = rows[i];

				if (stopPredicate != null && stopPredicate(row))
				{
					break;
				}

				dataRows.Add(row);
			}

			if (mergePredicate != null)
			{
				dataRows = MergeMultilineRows(dataRows, mergePredicate);
			}

			var strategy = alignmentConfigs != null 
				? (IColumnAlignmentStrategy)new MixedColumnAlignmentStrategy() 
				: new LeftAlignedColumnStrategy();
			var aligned = AlignToHeaderColumns(header, dataRows, alignmentConfigs ?? [], strategy);
			return (header, aligned);
		}

		private static PdfTableRow GetHeaders(string[] headerKeywords, PdfTableRow row)
		{
			// Build anchors based on the original header row tokens and keywords
			var anchors = BuildAnchors(row, headerKeywords);
			var headerTokens = new List<SingleWordToken>();

			for (int i = 0; i < headerKeywords.Length && i < anchors.Count; i++)
			{
				var keyword = headerKeywords[i];
				var anchorColumn = anchors[i];
				
				// Create a single token for each header keyword at the anchor position
				// This preserves the multi-word structure like "Transaction Type" as a single conceptual column
				var headerToken = new SingleWordToken(keyword, new Position(row.Page, row.Row, anchorColumn));
				headerTokens.Add(headerToken);
			}

			return new PdfTableRow(row.Page, row.Row, headerTokens);
		}

		private static List<PdfTableRowColumns> AlignToHeaderColumns(
			PdfTableRow header, 
			List<PdfTableRow> rows, 
			IReadOnlyList<ColumnAlignmentConfig> alignmentConfigs, 
			IColumnAlignmentStrategy strategy)
		{
			// The header parameter here already has the correct anchor positions from GetHeaders
			var anchors = header.Tokens.Where(t => t.BoundingBox != null)
				.Select(t => t.BoundingBox!.Column).ToList();

			// Use the strategy to calculate cutoff points between columns
			var cutoffs = strategy.CalculateColumnCutoffs(anchors, alignmentConfigs);

			var result = new List<PdfTableRowColumns>();

			foreach (var row in rows)
			{
				var columns = Enumerable.Range(0, anchors.Count).Select(_ => new List<SingleWordToken>()).ToList();

				foreach (var token in row.Tokens)
				{
					if (token.BoundingBox == null)
					{
						continue;
					}

					var columnIndex = strategy.FindColumnIndex(cutoffs, token.BoundingBox.Column, anchors, alignmentConfigs);
					if (columnIndex >= 0 && columnIndex < columns.Count)
					{
						columns[columnIndex].Add(token);
					}
				}

				result.Add(new PdfTableRowColumns(row.Page, row.Row, columns.Select(c => (IReadOnlyList<SingleWordToken>)c.OrderBy(t => t.BoundingBox!.Column).ToList()).ToList()));
			}

			return result;
		}

		private static List<int> BuildAnchors(PdfTableRow header, string[] headerKeywords)
		{
			var anchors = new List<int>();
			int searchStart = 0;

			foreach (var keyword in headerKeywords)
			{
				var parts = keyword.Split(' ', StringSplitOptions.RemoveEmptyEntries);
				int idx = FindSequence(header.Tokens, parts, searchStart);
				if (idx == -1)
				{
					idx = searchStart < header.Tokens.Count ? searchStart : header.Tokens.Count - 1;
				}

				var anchorToken = header.Tokens[idx];
				anchors.Add(anchorToken.BoundingBox?.Column ?? 0);
				searchStart = idx + parts.Length;
			}

			return anchors;
		}

		private static int FindSequence(IReadOnlyList<SingleWordToken> tokens, string[] parts, int start)
		{
			for (int i = start; i <= tokens.Count - parts.Length; i++)
			{
				bool match = true;
				for (int j = 0; j < parts.Length; j++)
				{
					if (!string.Equals(tokens[i + j].Text, parts[j], StringComparison.InvariantCultureIgnoreCase))
					{
						match = false;
						break;
					}
				}

				if (match)
				{
					return i;
				}
			}

			return -1;
		}

		private static List<PdfTableRow> MergeMultilineRows(List<PdfTableRow> rows, Func<PdfTableRow, PdfTableRow, bool> mergePredicate)
		{
			if (rows.Count == 0)
			{
				return rows;
			}

			var merged = new List<PdfTableRow>();
			PdfTableRow current = rows[0];

			for (int i = 1; i < rows.Count; i++)
			{
				var next = rows[i];
				if (mergePredicate(current, next))
				{
					current = new PdfTableRow(
						current.Page,
						current.Row,
						(IReadOnlyList<SingleWordToken>)current.Tokens.Concat(next.Tokens).ToList());
				}
				else
				{
					merged.Add(current);
					current = next;
				}
			}

			merged.Add(current);
			return merged;
		}

		private static bool RowContainsAll(PdfTableRow row, string[] headerKeywords)
		{
			return headerKeywords.All(h =>
			{
				if (string.IsNullOrWhiteSpace(h))
				{
					return true;
				}

				return row.Text.Contains(h, StringComparison.InvariantCultureIgnoreCase);
			});
		}

		/// <summary>
		/// Calculates cutoff points between columns to avoid splitting logical text units.
		/// Assumes left-aligned column content.
		/// </summary>
		/// <param name="anchors">Column anchor positions</param>
		/// <returns>List of cutoff positions</returns>
		[Obsolete("Use IColumnAlignmentStrategy.CalculateColumnCutoffs instead")]
		private static List<int> CalculateColumnCutoffs(IReadOnlyList<int> anchors)
		{
			if (anchors.Count == 0)
			{
				return [];
			}

			var cutoffs = new List<int>();

			// For left-aligned columns, place cutoffs very close to the next column's anchor
			// This assumes text starts at the anchor and should not extend much into the next column's space
			for (int i = 0; i < anchors.Count - 1; i++)
			{
				var leftAnchor = anchors[i];
				var rightAnchor = anchors[i + 1];
				
				// Place cutoff just before the next anchor with a small buffer
				// This gives minimal space for left-aligned content to extend
				var buffer = Math.Max(1, (rightAnchor - leftAnchor) / 20); // 5% buffer or minimum 1 unit
				var cutoff = rightAnchor - buffer;
				cutoffs.Add(cutoff);
			}

			// Add a final cutoff at a large value for the last column
			cutoffs.Add(int.MaxValue);

			return cutoffs;
		}

		/// <summary>
		/// Finds which column a token belongs to based on cutoff points
		/// </summary>
		/// <param name="cutoffs">Pre-calculated cutoff points</param>
		/// <param name="tokenColumn">Position of the token</param>
		/// <returns>Column index</returns>
		[Obsolete("Use IColumnAlignmentStrategy.FindColumnIndex instead")]
		private static int FindColumnIndexByCutoff(IReadOnlyList<int> cutoffs, int tokenColumn)
		{
			for (int i = 0; i < cutoffs.Count; i++)
			{
				if (tokenColumn < cutoffs[i])
				{
					return i;
				}
			}

			// Should not happen due to int.MaxValue cutoff, but fallback to last column
			return cutoffs.Count - 1;
		}

		/// <summary>
		/// Finds table rows while excluding footer content from the analysis.
		/// This is a convenience method that filters out footer tokens before processing the table.
		/// </summary>
		/// <param name="words">All words from the PDF</param>
		/// <param name="headerKeywords">Keywords that identify the table header</param>
		/// <param name="footerHeightThreshold">Distance from bottom of page to consider as footer area</param>
		/// <param name="stopPredicate">Optional predicate to stop processing rows</param>
		/// <param name="mergePredicate">Optional predicate to merge multi-line rows</param>
		/// <returns>List of table rows excluding footer content</returns>
		public static List<PdfTableRow> FindTableRowsIgnoringFooter(
			IEnumerable<SingleWordToken> words,
			string[] headerKeywords,
			int footerHeightThreshold = 50,
			Func<PdfTableRow, bool>? stopPredicate = null,
			Func<PdfTableRow, PdfTableRow, bool>? mergePredicate = null)
		{
			var filteredWords = PdfToWordsParser.FilterOutFooter(words.ToList(), footerHeightThreshold);
			return FindTableRows(filteredWords, headerKeywords, stopPredicate, mergePredicate);
		}

		/// <summary>
		/// Finds table rows with columns while excluding footer content from the analysis.
		/// This is a convenience method that filters out footer tokens before processing the table.
		/// </summary>
		/// <param name="words">All words from the PDF</param>
		/// <param name="headerKeywords">Keywords that identify the table header</param>
		/// <param name="footerHeightThreshold">Distance from bottom of page to consider as footer area</param>
		/// <param name="stopPredicate">Optional predicate to stop processing rows</param>
		/// <param name="mergePredicate">Optional predicate to merge multi-line rows</param>
		/// <returns>Tuple containing the header and list of table rows with columns, excluding footer content</returns>
		public static (PdfTableRow Header, List<PdfTableRowColumns> Rows) FindTableRowsWithColumnsIgnoringFooter(
			IEnumerable<SingleWordToken> words,
			string[] headerKeywords,
			int footerHeightThreshold = 50,
			Func<PdfTableRow, bool>? stopPredicate = null,
			Func<PdfTableRow, PdfTableRow, bool>? mergePredicate = null)
		{
			var filteredWords = PdfToWordsParser.FilterOutFooter(words.ToList(), footerHeightThreshold);
			return FindTableRowsWithColumns(filteredWords, headerKeywords, stopPredicate, mergePredicate);
		}

		/// <summary>
		/// Finds table rows with columns while excluding footer content from the analysis.
		/// This overload supports custom column alignment configurations.
		/// </summary>
		/// <param name="words">All words from the PDF</param>
		/// <param name="headerKeywords">Keywords that identify the table header</param>
		/// <param name="alignmentConfigs">Per-column alignment configurations</param>
		/// <param name="footerHeightThreshold">Distance from bottom of page to consider as footer area</param>
		/// <param name="stopPredicate">Optional predicate to stop processing rows</param>
		/// <param name="mergePredicate">Optional predicate to merge multi-line rows</param>
		/// <returns>Tuple containing the header and list of table rows with columns, excluding footer content</returns>
		public static (PdfTableRow Header, List<PdfTableRowColumns> Rows) FindTableRowsWithColumnsIgnoringFooter(
			IEnumerable<SingleWordToken> words,
			string[] headerKeywords,
			IReadOnlyList<ColumnAlignmentConfig>? alignmentConfigs,
			int footerHeightThreshold = 50,
			Func<PdfTableRow, bool>? stopPredicate = null,
			Func<PdfTableRow, PdfTableRow, bool>? mergePredicate = null)
		{
			var filteredWords = PdfToWordsParser.FilterOutFooter(words.ToList(), footerHeightThreshold);
			return FindTableRowsWithColumns(filteredWords, headerKeywords, alignmentConfigs, stopPredicate, mergePredicate);
		}
	}
}
