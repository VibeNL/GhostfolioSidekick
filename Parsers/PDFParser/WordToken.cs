namespace GhostfolioSidekick.Parsers.PDFParser
{
	public record WordToken
	{
		public WordToken()
		{			
		}

		public WordToken(BoundingBox? boundingBox)
		{
			BoundingBox = boundingBox;
		}

		public BoundingBox? BoundingBox { get; }
	}
}