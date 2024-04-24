using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.Parsers.PDFParser.PdfToWords
{
	public interface IPdfToWordsParser
	{
		List<SingleWordToken> ParseTokens(string filePath);
	}
}
