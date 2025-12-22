using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.ISIN;
using GhostfolioSidekick.Parsers.PDFParser;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using System.Globalization;

namespace GhostfolioSidekick.Parsers.TradeRepublic
{
	public abstract class TradeRepublicInvoiceParserBase(IPdfToWordsParser parsePDfToWords) : PdfBaseParser(parsePDfToWords)
	{
		protected abstract string[] HeaderKeywords { get; } 

		protected abstract string StopWord { get; }

		protected override bool IgnoreFooter => true;

		protected override int FooterHeightThreshold => 50;

		protected override bool CanParseRecords(List<SingleWordToken> words)
		{
			var foundTradeRepublic = false;
			var foundSecurities = false;
			var foundLanguage = false;

			for (int i = 0; i < words.Count; i++)
			{
				if (IsCheckWords("TRADE REPUBLIC BANK GMBH", words, i, true))
				{
					foundTradeRepublic = true;
				}

			}

			return foundLanguage && foundTradeRepublic && foundSecurities;
		}

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Critical Code Smell", "S3776:Cognitive Complexity of methods should not be too high", Justification = "Not needed for now")]
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S127:\"for\" loop stop conditions should be invariant", Justification = "Needed for parser")]
		protected override List<PartialActivity> ParseRecords(List<SingleWordToken> words)
		{
			var activities = new List<PartialActivity>();

			bool StopPredicate(PdfTableRow row) => row.Text.Contains(StopWord, StringComparison.InvariantCultureIgnoreCase);

			bool MergePredicate(PdfTableRow current, PdfTableRow next)
			{
				// Merge if the first column of the next row is filled (has content)
				if (next.Tokens.Count == 0)
				{
					return false;
				}

				// Get the first token of the next row
				var firstToken = next.Tokens.FirstOrDefault();
				if (firstToken?.BoundingBox == null)
				{
					return false;
				}

				// Check if we have a current row to compare against
				if (current.Tokens.Count == 0)
				{
					return false;
				}

				// Get the first token's column position from the next row
				var firstTokenColumn = firstToken.BoundingBox.Column;

				// Get the first column position from the current row for comparison
				var currentFirstColumnPosition = current.Tokens
					.Where(t => t.BoundingBox != null)
					.FirstOrDefault()?.BoundingBox?.Column;

				if (!currentFirstColumnPosition.HasValue)
				{
					return false;
				}

				// Check if the first token of the next row aligns with the first column position
				// If it does, it means the first column is filled and we should merge
				var distanceToFirstColumn = Math.Abs(firstTokenColumn - currentFirstColumnPosition.Value);
				
				// Use a small tolerance for alignment (tokens might not be perfectly aligned)
				const int alignmentTolerance = 10;
				
				// Merge if the first token of next row is aligned with the first column
				return distanceToFirstColumn <= alignmentTolerance;
			}

			var (header, rows) = PdfTableExtractor.FindTableRowsWithColumns(
				words,
				HeaderKeywords,
				[
					new(0, ColumnAlignment.Left),   // POSITION
					new(1, ColumnAlignment.Left),   // QUANTITY  
					new(2, ColumnAlignment.Left),   // PRICE
					new(3, ColumnAlignment.Right)   // AMOUNT (right-aligned)
				],
				stopPredicate: StopPredicate,
				mergePredicate: MergePredicate);

			foreach (var row in rows)
			{
				var parsed = ParseRecord(header, row);
				if (parsed != null)
				{
					activities.AddRange(parsed);
				}
			}

			return activities;
		}

		private IEnumerable<PartialActivity> ParseRecord(PdfTableRow header, PdfTableRowColumns row)
		{
			throw new NotImplementedException();
		}
	}
}
