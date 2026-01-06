namespace GhostfolioSidekick.Parsers.PDFParser.PdfToWords
{
	/// <summary>
	/// Represents the alignment type for a column
	/// </summary>
	public enum ColumnAlignment
	{
		Left,
		Right
	}

	/// <summary>
	/// Strategy interface for column alignment algorithms
	/// </summary>
	public interface IColumnAlignmentStrategy
	{
		/// <summary>
		/// Calculates cutoff points between columns based on alignment configuration
		/// </summary>
		/// <param name="anchors">Column anchor positions from headers</param>
		/// <param name="alignmentConfigs">Per-column alignment configurations</param>
		/// <returns>List of cutoff positions for token assignment</returns>
		List<int> CalculateColumnCutoffs(IReadOnlyList<int> anchors, IReadOnlyList<ColumnAlignment> alignmentConfigs);

		/// <summary>
		/// Determines which column a token belongs to based on its position and alignment strategy
		/// </summary>
		/// <param name="cutoffs">Pre-calculated cutoff points</param>
		/// <param name="tokenColumn">Position of the token</param>
		/// <param name="anchors">Column anchor positions</param>
		/// <param name="alignmentConfigs">Per-column alignment configurations</param>
		/// <returns>Column index for the token</returns>
		int FindColumnIndex(IReadOnlyList<int> cutoffs, int tokenColumn, IReadOnlyList<int> anchors, IReadOnlyList<ColumnAlignment> alignmentConfigs);
	}

	/// <summary>
	/// Default column alignment strategy supporting mixed left/right/center alignment
	/// </summary>
	public sealed class MixedColumnAlignmentStrategy : IColumnAlignmentStrategy
	{
		public List<int> CalculateColumnCutoffs(IReadOnlyList<int> anchors, IReadOnlyList<ColumnAlignment> alignmentConfigs)
		{
			if (anchors.Count == 0)
			{
				return [];
			}

			var cutoffs = new List<int>();

			for (int i = 0; i < anchors.Count - 1; i++)
			{
				var leftAnchor = anchors[i];
				var rightAnchor = anchors[i + 1];
				var leftAlignment = GetAlignmentForColumn(i, alignmentConfigs);
				var rightAlignment = GetAlignmentForColumn(i + 1, alignmentConfigs);

				var cutoff = CalculateCutoffBetweenColumns(leftAnchor, rightAnchor, leftAlignment, rightAlignment);
				cutoffs.Add(cutoff);
			}

			// Add a final cutoff at a large value for the last column
			cutoffs.Add(int.MaxValue);

			return cutoffs;
		}

		public int FindColumnIndex(IReadOnlyList<int> cutoffs, int tokenColumn, IReadOnlyList<int> anchors, IReadOnlyList<ColumnAlignment> alignmentConfigs)
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

		private static ColumnAlignment GetAlignmentForColumn(int columnIndex, IReadOnlyList<ColumnAlignment> alignmentConfigs)
		{
			if (columnIndex < 0 || columnIndex >= alignmentConfigs.Count)
			{
				// Default to left alignment if out of bounds
				return ColumnAlignment.Left;
			}

			return alignmentConfigs[columnIndex];
		}

		private static int CalculateCutoffBetweenColumns(int leftAnchor, int rightAnchor, ColumnAlignment leftAlignment, ColumnAlignment rightAlignment)
		{
			var columnSpan = rightAnchor - leftAnchor;

			// Use 70% separation for left vs right alignment cases
			if ((leftAlignment == ColumnAlignment.Left && rightAlignment == ColumnAlignment.Right) ||
				(leftAlignment == ColumnAlignment.Right && rightAlignment == ColumnAlignment.Left))
			{
				return leftAnchor + (columnSpan * 7 / 10);
			}

			// For all other cases, use a simple 5% buffer from the right anchor
			return rightAnchor - Math.Max(1, columnSpan / 20);
		}
	}

	/// <summary>
	/// Legacy column alignment strategy that assumes all columns are left-aligned
	/// This maintains backward compatibility with the original behavior
	/// </summary>
	public sealed class LeftAlignedColumnStrategy : IColumnAlignmentStrategy
	{
		public List<int> CalculateColumnCutoffs(IReadOnlyList<int> anchors, IReadOnlyList<ColumnAlignment> alignmentConfigs)
		{
			if (anchors.Count == 0)
			{
				return [];
			}

			var cutoffs = new List<int>();

			// Original logic: assumes all columns are left-aligned
			for (int i = 0; i < anchors.Count - 1; i++)
			{
				var leftAnchor = anchors[i];
				var rightAnchor = anchors[i + 1];

				// Place cutoff just before the next anchor with a small buffer
				var buffer = Math.Max(1, (rightAnchor - leftAnchor) / 20); // 5% buffer or minimum 1 unit
				var cutoff = rightAnchor - buffer;
				cutoffs.Add(cutoff);
			}

			cutoffs.Add(int.MaxValue);
			return cutoffs;
		}

		public int FindColumnIndex(IReadOnlyList<int> cutoffs, int tokenColumn, IReadOnlyList<int> anchors, IReadOnlyList<ColumnAlignment> alignmentConfigs)
		{
			for (int i = 0; i < cutoffs.Count; i++)
			{
				if (tokenColumn < cutoffs[i])
				{
					return i;
				}
			}

			return cutoffs.Count - 1;
		}
	}
}