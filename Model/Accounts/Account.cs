using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Accounts
{
	public class Account
	{
		public Account(string name, Balance balance)
		{
			Name = name;
			Balance = balance;
		}

		public string Name { get; set; }

		public Balance Balance { get; set; }

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
