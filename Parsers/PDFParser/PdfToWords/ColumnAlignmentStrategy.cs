using System.Collections.Generic;

namespace GhostfolioSidekick.Parsers.PDFParser.PdfToWords
{
	/// <summary>
	/// Represents the alignment type for a column
	/// </summary>
	public enum ColumnAlignment
	{
		Left,
		Right,
		Center
	}

	/// <summary>
	/// Configuration for a specific column's alignment behavior
	/// </summary>
	public sealed record ColumnAlignmentConfig(int ColumnIndex, ColumnAlignment Alignment);

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
		List<int> CalculateColumnCutoffs(IReadOnlyList<int> anchors, IReadOnlyList<ColumnAlignmentConfig> alignmentConfigs);

		/// <summary>
		/// Determines which column a token belongs to based on its position and alignment strategy
		/// </summary>
		/// <param name="cutoffs">Pre-calculated cutoff points</param>
		/// <param name="tokenColumn">Position of the token</param>
		/// <param name="anchors">Column anchor positions</param>
		/// <param name="alignmentConfigs">Per-column alignment configurations</param>
		/// <returns>Column index for the token</returns>
		int FindColumnIndex(IReadOnlyList<int> cutoffs, int tokenColumn, IReadOnlyList<int> anchors, IReadOnlyList<ColumnAlignmentConfig> alignmentConfigs);
	}

	/// <summary>
	/// Default column alignment strategy supporting mixed left/right/center alignment
	/// </summary>
	public sealed class MixedColumnAlignmentStrategy : IColumnAlignmentStrategy
	{
		public List<int> CalculateColumnCutoffs(IReadOnlyList<int> anchors, IReadOnlyList<ColumnAlignmentConfig> alignmentConfigs)
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

		public int FindColumnIndex(IReadOnlyList<int> cutoffs, int tokenColumn, IReadOnlyList<int> anchors, IReadOnlyList<ColumnAlignmentConfig> alignmentConfigs)
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

		private static ColumnAlignment GetAlignmentForColumn(int columnIndex, IReadOnlyList<ColumnAlignmentConfig> alignmentConfigs)
		{
			var config = alignmentConfigs.FirstOrDefault(c => c.ColumnIndex == columnIndex);
			return config?.Alignment ?? ColumnAlignment.Left; // Default to left alignment
		}

		private static int CalculateCutoffBetweenColumns(int leftAnchor, int rightAnchor, ColumnAlignment leftAlignment, ColumnAlignment rightAlignment)
		{
			var columnSpan = rightAnchor - leftAnchor;

			return (leftAlignment, rightAlignment) switch
			{
				// Left column gets more space, right column starts closer to its anchor
				(ColumnAlignment.Left, ColumnAlignment.Left) => rightAnchor - Math.Max(1, columnSpan / 20), // 5% buffer
				(ColumnAlignment.Left, ColumnAlignment.Right) => rightAnchor - Math.Max(1, columnSpan / 10), // 10% buffer for right-aligned
				(ColumnAlignment.Left, ColumnAlignment.Center) => rightAnchor - Math.Max(1, columnSpan / 8), // 12.5% buffer for center

				// Right column gets more space, split closer to middle
				(ColumnAlignment.Right, ColumnAlignment.Left) => leftAnchor + (columnSpan * 3 / 4), // 75% to left column
				(ColumnAlignment.Right, ColumnAlignment.Right) => leftAnchor + (columnSpan * 2 / 3), // 66% to left column
				(ColumnAlignment.Right, ColumnAlignment.Center) => leftAnchor + (columnSpan * 3 / 5), // 60% to left column

				// Center column cases
				(ColumnAlignment.Center, ColumnAlignment.Left) => rightAnchor - Math.Max(1, columnSpan / 6), // 16% buffer
				(ColumnAlignment.Center, ColumnAlignment.Right) => leftAnchor + (columnSpan / 2), // Split in middle
				(ColumnAlignment.Center, ColumnAlignment.Center) => leftAnchor + (columnSpan / 2), // Split in middle

				_ => rightAnchor - Math.Max(1, columnSpan / 20) // Default fallback
			};
		}
	}

	/// <summary>
	/// Legacy column alignment strategy that assumes all columns are left-aligned
	/// This maintains backward compatibility with the original behavior
	/// </summary>
	public sealed class LeftAlignedColumnStrategy : IColumnAlignmentStrategy
	{
		public List<int> CalculateColumnCutoffs(IReadOnlyList<int> anchors, IReadOnlyList<ColumnAlignmentConfig> alignmentConfigs)
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

		public int FindColumnIndex(IReadOnlyList<int> cutoffs, int tokenColumn, IReadOnlyList<int> anchors, IReadOnlyList<ColumnAlignmentConfig> alignmentConfigs)
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