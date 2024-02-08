using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Accounts
{
	public class Account(string name, Balance balance)
	{
		public string Name { get; set; } = name;

		public Balance Balance { get; set; } = balance;

		public string? Id { get; set; }

		public string? Comment { get; set; }

		public Platform? Platform { get; set; }

		[ExcludeFromCodeCoverage]
		public override string ToString()
		{
			return Name;
		}
	}
}
