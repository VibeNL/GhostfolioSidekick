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
			Func<PdfTableRow, bool>? startPredicate = null,
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
			bool started = startPredicate is null;
			for (int i = headerIndex + 1; i < rows.Count; i++)
			{
				var row = rows[i];

				if (!started && startPredicate != null)
				{
					if (!startPredicate(row))
					{
						continue;
					}
					started = true;
				}

				if (stopPredicate != null && stopPredicate(row))
				{
					break;
				}

				if (started)
				{
					dataRows.Add(row);
				}
			}

			if (mergePredicate != null)
			{
				dataRows = MergeMultilineRows(dataRows, mergePredicate);
			}

			var aligned = AlignToHeaderColumns(header, dataRows);
			return (header, aligned);
		}

		private static List<PdfTableRowColumns> AlignToHeaderColumns(PdfTableRow header, List<PdfTableRow> rows)
		{
			var anchors = header.Tokens
				.Where(t => t.BoundingBox != null)
				.OrderBy(t => t.BoundingBox!.Column)
				.Select(t => t.BoundingBox!.Column)
				.ToArray();

			var result = new List<PdfTableRowColumns>();

			foreach (var row in rows)
			{
				var columns = Enumerable.Range(0, anchors.Length).Select(_ => new List<SingleWordToken>()).ToList();

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

		private static int FindNearestAnchorIndex(int[] anchors, int column)
		{
			var nearest = 0;
			var minDiff = int.MaxValue;
			for (int i = 0; i < anchors.Length; i++)
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
	}
}
