using AwesomeAssertions;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;

namespace GhostfolioSidekick.Tools.AnonymisePDF.UnitTests
{
	public class ProgramTests
	{
		[Fact]
		public void Main_WhenCalledWithValidArguments_CallsPdfCleanerAutoSweepCleanUp()
		{
			// Arrange
			if (File.Exists("result.pdf"))
			{
				File.Delete("result.pdf");
			}

			var args = new string[] { "test.pdf", "result.pdf", "Adobe Acrobat Reader", "computer" };

			// Act
			Program.Main(args);

			// Assert
			// use itext7 to read the result.pdf and check that the words "Adobe Acrobat Reader" and "computer" are redacted

			// Load the PDF file
			using PdfDocument pdfDocument = new(new PdfReader("result.pdf"));
			// Extract the text content from the PDF
			string extractedText = PdfTextExtractor.GetTextFromPage(pdfDocument.GetPage(1));

			// Check if the words are redacted
			bool isAdobeAcrobatReaderRedacted = extractedText.Contains("Adobe Acrobat Reader");
			bool isComputerRedacted = extractedText.Contains("computer");

			// Assert the results
			isAdobeAcrobatReaderRedacted.Should().BeFalse();
			isComputerRedacted.Should().BeFalse();

			// Close the PDF document
			pdfDocument.Close();
		}
	}
}
