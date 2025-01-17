namespace ScraperUtilities
{
	public record CommandLineArguments(string Broker, string Username, string Password)
	{
		internal static CommandLineArguments Parse(string[] args)
		{
			return new CommandLineArguments(args[0], args[1], args[2]);
		}
	}
}