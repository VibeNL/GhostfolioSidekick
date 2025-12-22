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

			var header = rows[headerIndex];

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

			var aligned = AlignToHeaderColumns(header, dataRows, headerKeywords);
			return (header, aligned);
		}

		private static List<PdfTableRowColumns> AlignToHeaderColumns(PdfTableRow header, List<PdfTableRow> rows, string[] headerKeywords)
		{
			var anchors = BuildAnchors(header, headerKeywords);

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

					var idx = FindNearestAnchorIndex(anchors, token.BoundingBox.Column);
					columns[idx].Add(token);
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

		private static int FindNearestAnchorIndex(IReadOnlyList<int> anchors, int column)
		{
			var nearest = 0;
			var minDiff = int.MaxValue;
			for (int i = 0; i < anchors.Count; i++)
			{
				var diff = Math.Abs(anchors[i] - column);
				if (diff < minDiff)
				{
					minDiff = diff;
					nearest = i;
				}
			}

			return nearest;
		}
	}
}
