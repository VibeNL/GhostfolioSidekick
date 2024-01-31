namespace GhostfolioSidekick.Model.Accounts
{
	public class Platform(string name)
	{
		public string Name { get; set; } = name;

		public string? Url { get; set; }

		public string? Id { get; set; }
	}
}