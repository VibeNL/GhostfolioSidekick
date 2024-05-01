namespace GhostfolioSidekick.Parsers.PDFParser
{
	public class SingleWordToken : IWordToken
	{
		public SingleWordToken(string text)
		{
			Text = text;
		}

		public string Text { get; }
	}
}
