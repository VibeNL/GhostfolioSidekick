using AwesomeAssertions;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Parsers.PDFParser;
using GhostfolioSidekick.Parsers.PDFParser.PdfToWords;
using Moq;

namespace GhostfolioSidekick.Parsers.UnitTests.PDFParser
{
	public class PdfBaseParserTests
	{
		private class TestPdfBaseParser(IPdfToWordsParser parsePDfToWords) : PdfBaseParser(parsePDfToWords)
		{
			protected override bool CanParseRecords(List<SingleWordToken> words)
			{
				return words.Count > 0;
			}

			protected override List<PartialActivity> ParseRecords(List<SingleWordToken> words)
			{
				return
				[
					new PartialActivity(PartialActivityType.Buy, DateTime.Now, Currency.USD, new Money(Currency.USD, 100), "txn1")
				];
			}

			public static bool IsCheckWordsPublic(string check, List<SingleWordToken> words, int i)
			{
				return IsCheckWords(check, words, i);
			}
		}

		[Fact]
		public async Task CanParse_ReturnsFalse_WhenFileExtensionIsNotPdf()
		{
			var mockPdfToWordsParser = new Mock<IPdfToWordsParser>();
			var parser = new TestPdfBaseParser(mockPdfToWordsParser.Object);

			var result = await parser.CanParse("test.txt");

			result.Should().BeFalse();
		}

		[Fact]
		public async Task CanParse_ReturnsFalse_WhenExceptionIsThrown()
		{
			var mockPdfToWordsParser = new Mock<IPdfToWordsParser>();
			mockPdfToWordsParser.Setup(p => p.ParseTokens(It.IsAny<string>())).Throws(new Exception("Test exception"));
			var parser = new TestPdfBaseParser(mockPdfToWordsParser.Object);

			var result = await parser.CanParse("test.pdf");

			result.Should().BeFalse();
		}

		[Fact]
		public async Task CanParse_ReturnsTrue_WhenPdfIsValid()
		{
			var mockPdfToWordsParser = new Mock<IPdfToWordsParser>();
			mockPdfToWordsParser.Setup(p => p.ParseTokens(It.IsAny<string>())).Returns([new SingleWordToken("test")]);
			var parser = new TestPdfBaseParser(mockPdfToWordsParser.Object);

			var result = await parser.CanParse("test.pdf");

			result.Should().BeTrue();
		}

		[Fact]
		public async Task ParseActivities_ParsesRecordsCorrectly()
		{
			var mockPdfToWordsParser = new Mock<IPdfToWordsParser>();
			mockPdfToWordsParser.Setup(p => p.ParseTokens(It.IsAny<string>())).Returns([new SingleWordToken("test")]);
			var parser = new TestPdfBaseParser(mockPdfToWordsParser.Object);
			var mockActivityManager = new Mock<IActivityManager>();

			await parser.ParseActivities("test.pdf", mockActivityManager.Object, "account1");

			mockActivityManager.Verify(m => m.AddPartialActivity(It.IsAny<string>(), It.IsAny<IEnumerable<PartialActivity>>()), Times.Once);
		}

		[Fact]
		public void IsCheckWords_ReturnsTrue_WhenWordsMatch()
		{
			var words = new List<SingleWordToken>
			{
				new SingleWordToken("hello"),
				new SingleWordToken("world")
			};

			var result = TestPdfBaseParser.IsCheckWordsPublic("hello world", words, 0);

			result.Should().BeTrue();
		}

		[Fact]
		public void IsCheckWords_ReturnsFalse_WhenWordsDoNotMatch()
		{
			var words = new List<SingleWordToken>
			{
				new SingleWordToken("hello"),
				new SingleWordToken("world")
			};

			var result = TestPdfBaseParser.IsCheckWordsPublic("hello there", words, 0);

			result.Should().BeFalse();
		}
	}
}
