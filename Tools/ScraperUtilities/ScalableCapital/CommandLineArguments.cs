namespace ScraperUtilities.ScalableCapital
{
	public record CommandLineArguments : ScraperUtilities.CommandLineArguments
	{
		public CommandLineArguments(string[] args) : base(args)
		{
		}

		public string Username => args[2];

		public string Password => args[3];
	}
}
