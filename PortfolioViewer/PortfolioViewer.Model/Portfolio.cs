namespace PortfolioViewer.Model
{
	public class Portfolio
	{
		public ICollection<Account> Accounts { get; set; }

		public ICollection<Activity> Activities { get; set; }

		public ICollection<Holding> Holdings { get; set; }

		public ICollection<SymbolProfile> SymbolProfiles { get; set; }
	}
}
