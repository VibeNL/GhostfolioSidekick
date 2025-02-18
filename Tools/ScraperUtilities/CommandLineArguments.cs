namespace ScraperUtilities
{
	public record CommandLineArguments(
		string Broker, 
		string Username, 
		string Password, 
		string OutputFile)
	{
		public string Portfolio { get; internal set; }

		internal static CommandLineArguments Parse(string[] args)
		{
			var c = new CommandLineArguments(args[0], args[1], args[2], args[3]);

			if (args.Length > 4)
			{
				c.Portfolio = args[4];
			}

			return c;
		}
	}
}