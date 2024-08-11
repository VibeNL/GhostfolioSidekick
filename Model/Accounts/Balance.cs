using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model.Accounts
{
	public class Balance
	{
		internal Balance()
		{
			// EF Core
			DateTime = default!;
			Money = default!;
		}

		public Balance(DateTime dateTime, Money money)
		{
			DateTime = dateTime;
			Money = money;
		}

		public int Id { get; set; }

		public DateTime DateTime { get; }

		public Money Money { get; set; }

		[ExcludeFromCodeCoverage]
		public override string ToString()
		{
			return DateTime.ToInvariantString() + " " + Money.ToString();
		}
	}
}