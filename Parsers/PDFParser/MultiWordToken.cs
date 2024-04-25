

namespace GhostfolioSidekick.Parsers.PDFParser
{
	public class MultiWordToken : WordToken
	{
		public MultiWordToken(string keyWord)
		{
			KeyWord = keyWord;
		}

		public string KeyWord { get; }

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
