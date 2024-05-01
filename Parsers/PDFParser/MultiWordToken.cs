

namespace GhostfolioSidekick.Parsers.PDFParser
{
	public class MultiWordToken : IWordToken
	{
		public MultiWordToken(string keyWord)
		{
			KeyWord = keyWord;
		}

		public string KeyWord { get; }

		public List<IWordToken> Words { get; } = new List<IWordToken>();

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
