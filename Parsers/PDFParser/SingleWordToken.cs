namespace GhostfolioSidekick.Parsers.PDFParser
{
	public class SingleWordToken : WordToken
	{
		public SingleWordToken(string text)
		{
			Text = text;
		}

		public string Text { get; }
	}
}
