namespace GhostfolioSidekick.Model.Accounts
{
	public class Balance
	{
		public Balance(DateTime dateTime, Money money)
		{
			DateTime = dateTime;
			Money = money;
		}

		public DateTime DateTime { get; }

		public Money Money { get; set; }

		public override string ToString()
		{
			return DateTime.ToInvariantString() + " " + Money.ToString();
		}
	}
}