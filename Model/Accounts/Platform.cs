namespace GhostfolioSidekick.Model.Accounts
{
	public class Platform
	{
		public Platform(string name)
		{
			Name = name;
		}

		public string Name { get; set; }

		public string? Url { get; set; }

		public string? Id { get; set; }
	}
}