namespace GhostfolioSidekick.Parsers.PDFParser
{
	public record Point
	{
		public Point(double x, double y)
		{
			X = x;
			Y = y;
		}

		public double X { get; }
		public double Y { get; }
	}
}
