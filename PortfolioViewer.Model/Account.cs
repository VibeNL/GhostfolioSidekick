namespace PortfolioViewer.Model
{
	public class Account
	{
		public string Name { get; set; }
		public string? Comment { get; set; }
		public Platform? Platform { get; set; }
		public ICollection<Balance> Balance { get; set; }
	}
}
