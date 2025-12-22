using AwesomeAssertions;
using System.Linq;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;

namespace GhostfolioSidekick.Parsers.UnitTests.PDFParser
{
	public class PdfTableExtractorTests
	{
		[Fact]
		public void GroupRows_GroupsByPageAndRow()
		{
			// Layout:
			// Page0 Row0: A (x=0), B (x=5)
			// Page0 Row1: C (x=1)
			// Page1 Row0: D (x=0)
			var words = new List<SingleWordToken>
			{
				Token("A", 0, 0, 0),
				Token("B", 0, 0, 5),
				Token("C", 0, 1, 1),
				Token("D", 1, 0, 0)
			};

			var rows = PdfTableExtractor.GroupRows(words);

			rows.Should().HaveCount(3);
			rows[0].Tokens.Select(t => t.Text).Should().ContainInOrder("A", "B");
			rows[1].Tokens.Select(t => t.Text).Should().ContainSingle().Which.Should().Be("C");
			rows[2].Tokens.Select(t => t.Text).Should().ContainSingle().Which.Should().Be("D");
		}

		[Fact]
		public void FindTableRowsWithColumns_AlignsByHeaderAnchors()
		{
			// Layout:
			// Header: H1 (x=0) | H2 (x=10) | H3 (x=20)
			// Row:    r1c1 (x?1) | r1c2 (x?11) | r1c3 (x?21)
			var header = new List<SingleWordToken>
			{
				Token("H1", 0, 0, 0),
				Token("H2", 0, 0, 10),
				Token("H3", 0, 0, 20),
			};

			var data = new List<SingleWordToken>
			{
				Token("r1c1", 0, 1, 1),
				Token("r1c2", 0, 1, 11),
				Token("r1c3", 0, 1, 21),
			};

			var (hdr, rows) = PdfTableExtractor.FindTableRowsWithColumns(
				header.Concat(data),
				["H1", "H2", "H3"],
				stopPredicate: null,
				mergePredicate: null);

			hdr.Tokens.Should().HaveCount(3);
			rows.Should().HaveCount(1);
			rows[0].Columns.Should().HaveCount(3);
			rows[0].Columns[0].Single().Text.Should().Be("r1c1");
			rows[0].Columns[1].Single().Text.Should().Be("r1c2");
			rows[0].Columns[2].Single().Text.Should().Be("r1c3");
		}

		[Fact]
		public void FindTableRowsWithColumns_MergesMultiLineRows()
		{
			// Layout:
			// Header: H1 (x=0) | H2 (x=10)
			// Row1: r1c1 (x?1) | partA (x?11)
			// Row2: r1c1b (x?2) | partB (x?12) -> merged with Row1
			var header = new List<SingleWordToken>
			{
				Token("H1", 0, 0, 0),
				Token("H2", 0, 0, 10)
			};

			var line1 = new List<SingleWordToken>
			{
				Token("r1c1", 0, 1, 1),
				Token("partA", 0, 1, 11)
			};

			var line2 = new List<SingleWordToken>
			{
				Token("r1c1b", 0, 2, 2),
				Token("partB", 0, 2, 12)
			};

			bool MergePredicate(PdfTableRow current, PdfTableRow next) => next.Page == current.Page && next.Row == current.Row + 1;

			var (hdr, rows) = PdfTableExtractor.FindTableRowsWithColumns(
				header.Concat(line1).Concat(line2),
				["H1", "H2"],
				stopPredicate: null,
				mergePredicate: MergePredicate);

			rows.Should().HaveCount(1);
			rows[0].Columns[0].Select(t => t.Text).Should().ContainInOrder("r1c1", "r1c1b");
			rows[0].Columns[1].Select(t => t.Text).Should().ContainInOrder("partA", "partB");
		}

		[Fact]
		public void FindTableRowsWithColumns_HandlesUnevenColumnSpacing()
		{
			// Layout:
			// Header: Date (x=0) | Description (x=15) | Amount (x=40)
			// Row:    2024-01-01 (x?1) | Long desc (x?16-17) | EUR 123.45 (x?41-42)
			var header = new List<SingleWordToken>
			{
				Token("Date", 0, 0, 0),
				Token("Description", 0, 0, 15),
				Token("Amount", 0, 0, 40)
			};

			var data = new List<SingleWordToken>
			{
				Token("2024-01-01", 0, 1, 1),
				Token("Long", 0, 1, 16),
				Token("desc", 0, 1, 17),
				Token("EUR", 0, 1, 41),
				Token("123.45", 0, 1, 42)
			};

			var (hdr, rows) = PdfTableExtractor.FindTableRowsWithColumns(
				header.Concat(data),
				["Date", "Description", "Amount"],
				stopPredicate: null,
				mergePredicate: null);

			hdr.Tokens.Select(t => t.Text).Should().ContainInOrder("Date", "Description", "Amount");
			rows.Should().HaveCount(1);
			rows[0].Columns.Should().HaveCount(3);
			rows[0].Columns[0].Single().Text.Should().Be("2024-01-01");
			rows[0].Columns[1].Select(t => t.Text).Should().ContainInOrder("Long", "desc");
			rows[0].Columns[2].Select(t => t.Text).Should().ContainInOrder("EUR", "123.45");
		}

		[Fact]
		public void FindTableRowsWithColumns_MergesMultiLineRows_WithMultipleColumns()
		{
			// Layout:
			// Header: Col1 (x=0) | Col2 (x=10) | Col3 (x=20)
			// Row1:   A1 (x?1) | B1 (x?11) | C1 (x?21)
			// Row2:   A1b (x?2) | B1b (x?12) | C1b (x?22)
			var header = new List<SingleWordToken>
			{
				Token("Col1", 0, 0, 0),
				Token("Col2", 0, 0, 10),
				Token("Col3", 0, 0, 20)
			};

			var line1 = new List<SingleWordToken>
			{
				Token("A1", 0, 1, 1),
				Token("B1", 0, 1, 11),
				Token("C1", 0, 1, 21)
			};

			var line2 = new List<SingleWordToken>
			{
				Token("A1b", 0, 2, 2),
				Token("B1b", 0, 2, 12),
				Token("C1b", 0, 2, 22)
			};

			bool MergePredicate(PdfTableRow current, PdfTableRow next) => next.Page == current.Page && next.Row == current.Row + 1;

			var (hdr, rows) = PdfTableExtractor.FindTableRowsWithColumns(
				header.Concat(line1).Concat(line2),
				["Col1", "Col2", "Col3"],
				stopPredicate: null,
				mergePredicate: MergePredicate);

			rows.Should().HaveCount(1);
			rows[0].Columns[0].Select(t => t.Text).Should().ContainInOrder("A1", "A1b");
			rows[0].Columns[1].Select(t => t.Text).Should().ContainInOrder("B1", "B1b");
			rows[0].Columns[2].Select(t => t.Text).Should().ContainInOrder("C1", "C1b");
		}

		private static SingleWordToken Token(string text, int page, int row, int column)
		{
			return new SingleWordToken(text, new Position(page, row, column));
		}
	}
}
