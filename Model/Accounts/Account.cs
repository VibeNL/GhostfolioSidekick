namespace GhostfolioSidekick.Model.Accounts
{
	public class Account
	{
		public Account()
		{
			// EF Core
			Name = null!;
			Balance = null!;
		}

		public Account(string name)
		{
			Name = name;
		}

		public string Name { get; set; }

		public virtual List<Balance> Balance { get; set; } = [];

		public int Id { get; set; }

		public string? Comment { get; set; }

		public virtual Platform? Platform { get; set; }

		public decimal GetTWR()
		{
			if (Balance == null || Balance.Count == 0)
			{
				return 0;
			}

			return Balance.Last().TWR;
		}

		public override string ToString()
		{
			return Name;
		}
	}
}
