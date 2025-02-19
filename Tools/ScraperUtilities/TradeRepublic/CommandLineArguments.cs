namespace ScraperUtilities.TradeRepublic
{
	public record CommandLineArguments : ScraperUtilities.CommandLineArguments
	{
		public CommandLineArguments(string[] args) : base(args)
		{
		}

		public string CountryCode => args[2];
		
		public string PhoneNumber => args[3];

		public string PinCode => args[4];
	}
}
