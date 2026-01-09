using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;

namespace GhostfolioSidekick.Examples
{
	/// <summary>
	/// Example showing how to use the column alignment strategy
	/// </summary>
	public static class ColumnAlignmentExample
	{
		/// <summary>
		/// Example for a PDF with 3 left-aligned columns and 1 right-aligned column
		/// </summary>
		public static void ProcessMixedAlignmentTable()
		{
			// Your PDF parsing code here...
			var words = GetWordsFromPdf(); // Replace with actual PDF parsing
			var headerKeywords = new[] { "Date", "Description", "Category", "Amount" };

			// Configure alignment: first 3 columns left-aligned, last column right-aligned
			var alignmentConfigs = new List<ColumnAlignmentConfig>
			{
				new(0, ColumnAlignment.Left),   // Date column
				new(1, ColumnAlignment.Left),   // Description column
				new(2, ColumnAlignment.Left),   // Category column
				new(3, ColumnAlignment.Right)   // Amount column (right-aligned)
			};

			// Extract table with custom alignment
			var (header, rows) = PdfTableExtractor.FindTableRowsWithColumns(
				words, 
				headerKeywords, 
				alignmentConfigs);

			// Process the extracted data
			foreach (var row in rows)
			{
				var date = row.GetColumnValue(header, "Date");
				var description = row.GetColumnValue(header, "Description");
				var category = row.GetColumnValue(header, "Category");
				var amount = row.GetColumnValue(header, "Amount");

				Console.WriteLine($"{date} | {description} | {category} | {amount}");
			}
		}

		/// <summary>
		/// Example for backward compatibility - uses the original left-aligned strategy
		/// </summary>
		public static void ProcessLegacyTable()
		{
			var words = GetWordsFromPdf(); // Replace with actual PDF parsing
			var headerKeywords = new[] { "Column1", "Column2", "Column3" };

			// This will use the legacy left-aligned strategy automatically
			var (header, rows) = PdfTableExtractor.FindTableRowsWithColumns(
				words, 
				headerKeywords);

			// Process normally...
		}

		/// <summary>
		/// Example with center-aligned columns
		/// </summary>
		public static void ProcessCenterAlignedTable()
		{
			var words = GetWordsFromPdf(); // Replace with actual PDF parsing
			var headerKeywords = new[] { "ID", "Name", "Status", "Score" };

			var alignmentConfigs = new List<ColumnAlignmentConfig>
			{
				new(0, ColumnAlignment.Center), // ID column
				new(1, ColumnAlignment.Left),   // Name column
				new(2, ColumnAlignment.Center), // Status column
				new(3, ColumnAlignment.Right)   // Score column
			};

			var (header, rows) = PdfTableExtractor.FindTableRowsWithColumns(
				words, 
				headerKeywords, 
				alignmentConfigs);

			// Process the data...
		}

		/// <summary>
		/// Example using the footer-ignoring method with alignment configurations
		/// </summary>
		public static void ProcessTableIgnoringFooter()
		{
			var words = GetWordsFromPdf(); // Replace with actual PDF parsing
			var headerKeywords = new[] { "Date", "Transaction", "Reference", "Amount" };

			// Configure alignment for your specific use case
			var alignmentConfigs = new List<ColumnAlignmentConfig>
			{
				new(0, ColumnAlignment.Left),   // Date column
				new(1, ColumnAlignment.Left),   // Transaction column  
				new(2, ColumnAlignment.Left),   // Reference column
				new(3, ColumnAlignment.Right)   // Amount column (right-aligned)
			};

			// Extract table with custom alignment, ignoring footer content
			var (header, rows) = PdfTableExtractor.FindTableRowsWithColumnsIgnoringFooter(
				words, 
				headerKeywords, 
				alignmentConfigs,
				footerHeightThreshold: 50);

			// Process the extracted data
			foreach (var row in rows)
			{
				var date = row.GetColumnValue(header, "Date");
				var transaction = row.GetColumnValue(header, "Transaction");
				var reference = row.GetColumnValue(header, "Reference");
				var amount = row.GetColumnValue(header, "Amount");

				Console.WriteLine($"{date} | {transaction} | {reference} | {amount}");
			}
		}

		private static IEnumerable<SingleWordToken> GetWordsFromPdf()
		{
			// Placeholder - replace with actual PDF parsing logic
			return Array.Empty<SingleWordToken>();
		}
	}
}