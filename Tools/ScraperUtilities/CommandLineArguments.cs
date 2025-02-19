namespace ScraperUtilities
{
	public record CommandLineArguments(string[] args)
	{
		public string Broker => args[0];

		public string OutputFile => args[1];
	}
}