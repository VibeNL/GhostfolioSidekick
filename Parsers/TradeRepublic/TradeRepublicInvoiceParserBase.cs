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
				// Merge if the first column of the next row is empty (indicating continuation of previous row)
				if (next.Tokens.Count == 0)
				{
					return false;
				}

				// Check if the first token of the next row has the same or very similar horizontal position as subsequent columns
				// This indicates that the first column is empty and this is a continuation row
				var firstToken = next.Tokens.FirstOrDefault();
				if (firstToken?.BoundingBox == null)
				{
					return false;
				}

				// Get the first token's column position
				var firstTokenColumn = firstToken.BoundingBox.Column;

				// If current row has tokens, check if the first token of next row aligns with non-first columns
				if (current.Tokens.Count > 1)
				{
					// Find the second column position from current row to determine if next row starts there
					var currentSecondColumnPosition = current.Tokens
						.Where(t => t.BoundingBox != null)
						.Skip(1)
						.FirstOrDefault()?.BoundingBox?.Column;

					if (currentSecondColumnPosition.HasValue)
					{
						// If the first token of next row is closer to the second column position,
						// it likely means the first column is empty
						var distanceToSecond = Math.Abs(firstTokenColumn - currentSecondColumnPosition.Value);
						var distanceToFirst = current.Tokens.FirstOrDefault()?.BoundingBox?.Column is int firstCol
							? Math.Abs(firstTokenColumn - firstCol)
							: int.MaxValue;

						return distanceToSecond < distanceToFirst;
					}
				}

				return false;
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
