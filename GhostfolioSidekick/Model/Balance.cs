namespace GhostfolioSidekick.Model
{
	public class Balance
	{
		public Balance(Money initial)
		{
			MoneyList.Add(initial);
			Currency = initial.Currency;
		}

		public Currency Currency { get; set; }

		public List<Money> MoneyList { get; private set; } = new List<Money>();

		public void Calculate(ICollection<Activity> newSet)
		{
			MoneyList.Clear();

			foreach (var activity in newSet.OrderBy(x => x.Date))
			{
				switch (activity.ActivityType)
				{
					case ActivityType.CashDeposit:
					case ActivityType.Dividend:
					case ActivityType.Interest:
					case ActivityType.Sell:
						MoneyList.Add(CalculateActivityTotal(activity));
						break;
					case ActivityType.CashWithdrawel:
					case ActivityType.Buy:

						MoneyList.Add(CalculateActivityTotal(activity).Negate());
						break;
					case ActivityType.Gift:
					case ActivityType.LearningReward:
					case ActivityType.Receive:
					case ActivityType.Send:
					case ActivityType.StakingReward:
						break;
					case ActivityType.Convert:
						throw new NotSupportedException();
					default:
						throw new NotSupportedException();
				}

				if (activity.Fee != null)
				{
					MoneyList.Add(activity.Fee.Negate());
				}
			}

			static Money CalculateActivityTotal(Activity activity)
			{
				return new Money(activity.UnitPrice.Currency, activity.UnitPrice.Amount * activity.Quantity, activity.UnitPrice.TimeOfRecord);
			}
		}
	}
}