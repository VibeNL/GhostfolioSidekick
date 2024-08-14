using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Accounts
{
	public class Platform
	{
		internal Platform()
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

		public string? Id { get; set; }

		public override string ToString()
		{
			return Name;
		}
	}
}