namespace GhostfolioSidekick.Parsers.PDFParser.PdfToWords
{
	public record TableDefinition(
		string[] Headers, 
		string StopWord, 
		ColumnAlignment[] ColumnAlignments, 
		bool IsRequired = false,
		IMergeRowStrategy? MergeStrategy = null);

	public sealed record PdfTableRow(string[] Headers, int Page, int Row, IReadOnlyList<SingleWordToken> Tokens)
	{
		public string Text => string.Join(" ", Tokens.Select(t => t.Text));

		internal bool IsHeader(string[] compareHeaders)
		{
			return
				compareHeaders.Length == Headers.Length &&
				compareHeaders.All(h =>
					!string.IsNullOrWhiteSpace(h) ||
					Headers.Any(rh => string.Equals(rh, h, StringComparison.InvariantCultureIgnoreCase)));
		}
	}

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
		public static List<PdfTableRow> GroupRows(IEnumerable<SingleWordToken> words, string[]? headers = null)
		{
			return [.. words
				.Where(w => w.BoundingBox != null)
				.GroupBy(w => new { w.BoundingBox!.Page, w.BoundingBox!.Row })
				.OrderBy(g => g.Key.Page)
				.ThenBy(g => g.Key.Row)
				.Select(g => new PdfTableRow(
					headers ?? Array.Empty<string>(),
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
			var definitions = tableDefinitions.ToList();
			var allResults = new List<PdfTableRow>();
			var usedRows = new HashSet<(int Page, int Row)>();

			// Check if all required table definitions are found
			var requiredDefinitions = definitions.Where(d => d.IsRequired).ToList();
			var foundRequiredDefinitions = new HashSet<TableDefinition>();

			foreach (var definition in definitions)
			{
				// Use the definition's merge strategy if available, otherwise fall back to the passed predicate
				var effectiveMergePredicate = GetEffectiveMergePredicate(definition, mergePredicate);
				
				var result = FindTableRowsForDefinition(words, definition, effectiveMergePredicate, usedRows);
				if (result.Count > 0)
				{
					allResults.AddRange(result);
					// Mark the rows as used to avoid overlapping tables
					foreach (var row in result)
					{
						usedRows.Add((row.Page, row.Row));
					}

					// Track found required definitions
					if (definition.IsRequired)
					{
						foundRequiredDefinitions.Add(definition);
					}
				}
			}

			// If not all required definitions were found, return empty result
			if (requiredDefinitions.Count > 0 && foundRequiredDefinitions.Count < requiredDefinitions.Count)
			{
				return [];
			}
			
			return allResults;
		}

		public static List<(PdfTableRow Header, List<PdfTableRowColumns> Rows, TableDefinition Definition)> FindAllTableRowsWithColumns(
			IEnumerable<SingleWordToken> words,
			IEnumerable<TableDefinition> tableDefinitions,
			Func<PdfTableRow, PdfTableRow, bool>? mergePredicate = null)
		{
			var definitions = tableDefinitions.ToList();
			var allResults = new List<(PdfTableRow Header, List<PdfTableRowColumns> Rows, TableDefinition Definition)>();
			var usedRows = new HashSet<(int Page, int Row)>();

			// Check if all required table definitions are found
			var requiredDefinitions = definitions.Where(d => d.IsRequired).ToList();
			var foundRequiredDefinitions = new HashSet<TableDefinition>();

			foreach (var definition in definitions)
			{
				// Use the definition's merge strategy if available, otherwise fall back to the passed predicate
				var effectiveMergePredicate = GetEffectiveMergePredicate(definition, mergePredicate);
				
				var result = FindTableRowsWithColumnsForDefinition(words, definition, effectiveMergePredicate, usedRows);
				if (result.Rows.Count > 0)
				{
					allResults.Add((result.Header, result.Rows, definition));
					// Mark the header and data rows as used to avoid overlapping tables
					usedRows.Add((result.Header.Page, result.Header.Row));
					foreach (var row in result.Rows)
					{
						usedRows.Add((row.Page, row.Row));
					}

					// Track found required definitions
					if (definition.IsRequired)
					{
						foundRequiredDefinitions.Add(definition);
					}
				}
			}

			// If not all required definitions were found, return empty result
			if (requiredDefinitions.Count > 0 && foundRequiredDefinitions.Count < requiredDefinitions.Count)
			{
				return [];
			}
			
			return allResults;
		}

		/// <summary>
		/// Finds table rows with columns, with explicit control over whether to return multiple tables.
		/// </summary>
		/// <param name="words">The word tokens to search</param>
		/// <param name="tableDefinitions">The table definitions to match</param>
		/// <param name="mergePredicate">Optional predicate for merging multi-line rows (fallback if TableDefinition doesn't have a strategy)</param>
		/// <param name="returnMultipleTables">If true, returns all matching tables combined; if false, returns first match only. If null, automatically detects based on number of table definitions.</param>
		/// <returns>Header and rows of the found table(s)</returns>
		public static List<PdfTableRowColumns> FindTableRowsWithColumns(
			IEnumerable<SingleWordToken> words,
			IEnumerable<TableDefinition> tableDefinitions,
			Func<PdfTableRow, PdfTableRow, bool>? mergePredicate = null,
			bool? returnMultipleTables = null)
		{
			var definitions = tableDefinitions.ToList();
			var allResults = FindAllTableRowsWithColumns(words, definitions, mergePredicate);
			
			if (allResults.Count == 0)
			{
				return new List<PdfTableRowColumns>();
			}

			// Determine if we should return multiple tables
			bool shouldReturnMultiple = returnMultipleTables ?? (definitions.Count > 1);

			if (shouldReturnMultiple && allResults.Count > 1)
			{
				// Combine all results into a single response
				// Use the first table's header as the primary header
				var primaryHeader = allResults[0].Header;
				var allRows = allResults.SelectMany(r => r.Rows).ToList();
				return allRows;
			}
			
			// Return first match only
			return allResults[0].Rows;
		}

		/// <summary>
		/// Gets the effective merge predicate to use for a table definition.
		/// Priority: TableDefinition.MergeStrategy -> passed mergePredicate -> null
		/// </summary>
		private static Func<PdfTableRow, PdfTableRow, bool>? GetEffectiveMergePredicate(
			TableDefinition definition, 
			Func<PdfTableRow, PdfTableRow, bool>? fallbackPredicate)
		{
			// If the table definition has a merge strategy, use it
			if (definition.MergeStrategy != null)
			{
				return definition.MergeStrategy.ShouldMerge;
			}

			// Otherwise, fall back to the passed predicate
			return fallbackPredicate;
		}

		private static List<PdfTableRow> FindTableRowsForDefinition(
			IEnumerable<SingleWordToken> words,
			TableDefinition definition,
			Func<PdfTableRow, PdfTableRow, bool>? mergePredicate,
			HashSet<(int Page, int Row)>? usedRows = null)
		{
			var rows = GroupRows(words);
			if (rows.Count == 0)
			{
				return [];
			}

			int headerIndex = rows.FindIndex(r => RowContainsAll(r, definition.Headers) && 
												  (usedRows == null || !usedRows.Contains((r.Page, r.Row))));
			if (headerIndex == -1)
			{
				return [];
			}

			var page = rows[headerIndex].Page;
			var result = new List<PdfTableRow>();
			
			for (int i = headerIndex + 1; i < rows.Count; i++)
			{
				var row = rows[i];
				
				// Skip if row is already used by another table
				if (usedRows != null && usedRows.Contains((row.Page, row.Row)))
				{
					continue;
				}
				
				if (row.Page != page)
				{
					break;
				}

				if (ShouldStopAtRow(row, definition.StopWord))
				{
					break;
				}

				// Create a new row with the proper headers from the table definition
				var rowWithHeaders = new PdfTableRow(
					definition.Headers,
					row.Page,
					row.Row,
					row.Tokens);

				result.Add(rowWithHeaders);
			}

			return mergePredicate == null ? result : MergeMultilineRows(result, mergePredicate, definition.Headers);
		}

		private static (PdfTableRow Header, List<PdfTableRowColumns> Rows) FindTableRowsWithColumnsForDefinition(
			IEnumerable<SingleWordToken> words,
			TableDefinition definition,
			Func<PdfTableRow, PdfTableRow, bool>? mergePredicate,
			HashSet<(int Page, int Row)>? usedRows = null)
		{
			var rows = GroupRows(words);
			if (rows.Count == 0)
			{
				return (new PdfTableRow(Array.Empty<string>(), 0, 0, Array.Empty<SingleWordToken>()), []);
			}

			int headerIndex = rows.FindIndex(r => RowContainsAll(r, definition.Headers) && 
												  (usedRows == null || !usedRows.Contains((r.Page, r.Row))));
			if (headerIndex == -1)
			{
				return (new PdfTableRow(Array.Empty<string>(), 0, 0, Array.Empty<SingleWordToken>()), []);
			}

			var header = GetHeaders(definition.Headers, rows[headerIndex]);
			var dataRows = new List<PdfTableRow>();
			
			for (int i = headerIndex + 1; i < rows.Count; i++)
			{
				var row = rows[i];

				// Skip if row is already used by another table
				if (usedRows != null && usedRows.Contains((row.Page, row.Row)))
				{
					continue;
				}

				if (ShouldStopAtRow(row, definition.StopWord))
				{
					break;
				}

				// Create a new row with the proper headers from the table definition
				var rowWithHeaders = new PdfTableRow(
					definition.Headers,
					row.Page,
					row.Row,
					row.Tokens);

				dataRows.Add(rowWithHeaders);
			}

			if (mergePredicate != null)
			{
				dataRows = MergeMultilineRows(dataRows, mergePredicate, definition.Headers);
			}

			var strategy = definition.ColumnAlignments?.Length > 0 
				? (IColumnAlignmentStrategy)new MixedColumnAlignmentStrategy() 
				: new LeftAlignedColumnStrategy();
			
			var aligned = AlignToHeaderColumns(header, dataRows, definition.ColumnAlignments ?? [], strategy);
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

			return new PdfTableRow(headerKeywords, row.Page, row.Row, headerTokens);
		}

		private static List<PdfTableRowColumns> AlignToHeaderColumns(
			PdfTableRow header, 
			List<PdfTableRow> rows, 
			IReadOnlyList<ColumnAlignment> alignmentConfigs, 
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

				// Create PdfTableRowColumns with header information
				result.Add(new PdfTableRowColumns(header.Headers, row.Page, row.Row, orderedColumns));
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

		private static List<PdfTableRow> MergeMultilineRows(List<PdfTableRow> rows, Func<PdfTableRow, PdfTableRow, bool> mergePredicate, string[] headers)
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
						headers, // Preserve headers during merge
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
