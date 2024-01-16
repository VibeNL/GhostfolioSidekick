using GhostfolioSidekick.Model.Accounts;

namespace GhostfolioSidekick.Model.Account
{
	public class Account(string name, Balance balance)
	{
		public string Name { get; set; } = name;

		public Balance Balance { get; set; } = balance;

		public string? Id { get; set; }

		public string? Comment { get; }

		public Platform? Platform { get; }
	}
}
