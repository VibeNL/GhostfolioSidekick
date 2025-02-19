namespace ScraperUtilities
{
	public record CommandLineArguments(
		string Broker, 
		string Username, 
		string Password, 
		string OutputFile)
	{
		internal static CommandLineArguments Parse(string[] args)
		{
			var c = new CommandLineArguments(args[0], args[1], args[2], args[3]);
			return c;
		}
	}
}