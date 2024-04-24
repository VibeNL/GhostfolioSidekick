

namespace GhostfolioSidekick.Parsers.PDFParser
{
	public record MultiWordToken : WordToken
	{
		public MultiWordToken(string keyWord)
		{
			KeyWord = keyWord;
		}

		public MultiWordToken(string keyWord, BoundingBox? box) : base(box)
		{
			KeyWord = keyWord;
		}

		public string KeyWord { get; }

		public bool IsMainLevel { get; }

		public List<WordToken> Words { get; } = new List<WordToken>();

		internal void AddMultiWord(MultiWordToken subWord)
		{
			// Add token to the list
			this.Words.Add(subWord);
		}

		internal void AddSingleWordToken(SingleWordToken token)
		{
			// Add token to the list
			this.Words.Add(token);
		}
	}
}
