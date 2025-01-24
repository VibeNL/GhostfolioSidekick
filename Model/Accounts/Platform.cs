namespace GhostfolioSidekick.Model.Accounts
{
	public class Platform
	{
		public Platform()
		{
			// EF Core
			Name = null!;
		}

		public Platform(string name)
		{
			Name = name;
		}

		public string Name { get; set; }

		public string? Url { get; set; }

		public int Id { get; set; }

		public override string ToString()
		{
			return Name;
		}
	}
}