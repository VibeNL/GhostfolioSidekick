using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Accounts
{
	public class Account
	{
		internal Account()
		{
			// EF Core
			Name = null!;
			Balance = null!;
		}

		public Account(string name, Balance balance)
		{
			Name = name;
			Balance = [balance];
		}

		public string Name { get; set; }

		public List<Balance> Balance { get; set; }

		public string? Id { get; set; }

		public string? Comment { get; set; }

		public Platform? Platform { get; set; }

		public override string ToString()
		{
			return Name;
		}
	}
}
