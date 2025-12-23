namespace GhostfolioSidekick.Parsers.PDFParser.PdfToWords
{
	public record TableDefinition(string[] Headers, string StopWord);

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
				.GroupBy(w => new { w.BoundingBox!.Page, w.BoundingBox!.Row })
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
			IEnumerable<TableDefinition> tableDefinitions,
			Func<PdfTableRow, PdfTableRow, bool>? mergePredicate = null)
		{
			foreach (var definition in tableDefinitions)
			{
				var result = FindTableRowsForDefinition(words, definition, mergePredicate);
				if (result.Count > 0)
				{
					return result;
				}
			}
			
			return [];
		}

		public static (PdfTableRow Header, List<PdfTableRowColumns> Rows) FindTableRowsWithColumns(
			IEnumerable<SingleWordToken> words,
			IEnumerable<TableDefinition> tableDefinitions,
			IReadOnlyList<ColumnAlignmentConfig> alignmentConfigs,
			Func<PdfTableRow, PdfTableRow, bool>? mergePredicate = null)
		{
			foreach (var definition in tableDefinitions)
			{
				var result = FindTableRowsWithColumnsForDefinition(words, definition, alignmentConfigs, mergePredicate);
				if (result.Rows.Count > 0)
				{
					return result;
				}
			}
			
			return (new PdfTableRow(0, 0, Array.Empty<SingleWordToken>()), []);
		}

		private static List<PdfTableRow> FindTableRowsForDefinition(
			IEnumerable<SingleWordToken> words,
			TableDefinition definition,
			Func<PdfTableRow, PdfTableRow, bool>? mergePredicate)
		{
			var rows = GroupRows(words);
			if (rows.Count == 0)
			{
				return [];
			}

			int headerIndex = rows.FindIndex(r => RowContainsAll(r, definition.Headers));
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

				if (ShouldStopAtRow(row, definition.StopWord))
				{
					break;
				}

				result.Add(row);
			}

			return mergePredicate == null ? result : MergeMultilineRows(result, mergePredicate);
		}

		private static (PdfTableRow Header, List<PdfTableRowColumns> Rows) FindTableRowsWithColumnsForDefinition(
			IEnumerable<SingleWordToken> words,
			TableDefinition definition,
			IReadOnlyList<ColumnAlignmentConfig> alignmentConfigs,
			Func<PdfTableRow, PdfTableRow, bool>? mergePredicate)
		{
			var rows = GroupRows(words);
			if (rows.Count == 0)
			{
				return (new PdfTableRow(0, 0, Array.Empty<SingleWordToken>()), []);
			}

			int headerIndex = rows.FindIndex(r => RowContainsAll(r, definition.Headers));
			if (headerIndex == -1)
			{
				return (new PdfTableRow(0, 0, Array.Empty<SingleWordToken>()), []);
			}

			var header = GetHeaders(definition.Headers, rows[headerIndex]);
			var dataRows = new List<PdfTableRow>();
			
			for (int i = headerIndex + 1; i < rows.Count; i++)
			{
				var row = rows[i];

				if (ShouldStopAtRow(row, definition.StopWord))
				{
					break;
				}

				dataRows.Add(row);
			}

			if (mergePredicate != null)
			{
				dataRows = MergeMultilineRows(dataRows, mergePredicate);
			}

			var strategy = alignmentConfigs?.Count > 0 
				? (IColumnAlignmentStrategy)new MixedColumnAlignmentStrategy() 
				: new LeftAlignedColumnStrategy();
			
			var aligned = AlignToHeaderColumns(header, dataRows, alignmentConfigs ?? [], strategy);
			return (header, aligned);
		}

		private static bool ShouldStopAtRow(PdfTableRow row, string? stopWord)
		{
			return !string.IsNullOrWhiteSpace(stopWord) && 
				   row.Text.Contains(stopWord, StringComparison.InvariantCultureIgnoreCase);
		}

		private static PdfTableRow GetHeaders(string[] headerKeywords, PdfTableRow row)
		{
			var anchors = BuildAnchors(row, headerKeywords);
			var headerTokens = new List<SingleWordToken>();

			for (int i = 0; i < headerKeywords.Length && i < anchors.Count; i++)
			{
				var keyword = headerKeywords[i];
				var anchorColumn = anchors[i];
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
			var anchors = header.Tokens
				.Where(t => t.BoundingBox != null)
				.Select(t => t.BoundingBox!.Column)
				.ToList();

			var cutoffs = strategy.CalculateColumnCutoffs(anchors, alignmentConfigs);
			var result = new List<PdfTableRowColumns>();

			foreach (var row in rows)
			{
				var columns = Enumerable.Range(0, anchors.Count)
					.Select(_ => new List<SingleWordToken>())
					.ToList();

				foreach (var token in row.Tokens.Where(t => t.BoundingBox != null))
				{
					var columnIndex = strategy.FindColumnIndex(cutoffs, token.BoundingBox!.Column, anchors, alignmentConfigs);
					if (columnIndex >= 0 && columnIndex < columns.Count)
					{
						columns[columnIndex].Add(token);
					}
				}

				var orderedColumns = columns
					.Select(c => (IReadOnlyList<SingleWordToken>)c.OrderBy(t => t.BoundingBox!.Column).ToList())
					.ToList();

				result.Add(new PdfTableRowColumns(row.Page, row.Row, orderedColumns));
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
						current.Tokens.Concat(next.Tokens).ToList());
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
				string.IsNullOrWhiteSpace(h) || 
				row.Text.Contains(h, StringComparison.InvariantCultureIgnoreCase));
		}
	}
}
