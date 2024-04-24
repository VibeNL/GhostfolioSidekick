namespace GhostfolioSidekick.Parsers.PDFParser.PdfToWords
{
	public class MultiWordToken : IWordToken
	{
		public MultiWordToken(string keyWord)
		{
			KeyWord = keyWord;
		}

		public MultiWordToken(string keyWord, Position? box) : base(box)
		{
			KeyWord = keyWord;
		}

		public string KeyWord { get; }

		public List<IWordToken> Words { get; } = new List<IWordToken>();

		internal void AddMultiWord(MultiWordToken subWord)
		{
			// Add token to the list
			Words.Add(subWord);
		}

		internal void AddSingleWordToken(SingleWordToken token)
		{
			// Add token to the list
			Words.Add(token);
		}
	}
}
