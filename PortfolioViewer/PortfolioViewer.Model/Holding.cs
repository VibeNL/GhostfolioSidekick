namespace PortfolioViewer.Model
{
	public class Holding
	{
		public int Id { get; set; }
		public ICollection<SymbolProfileId> Symbols { get; set; }
	}
}
