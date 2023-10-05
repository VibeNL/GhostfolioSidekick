namespace GhostfolioSidekick.Model
{
	public class Balance
	{
		private volatile Money _knownBalance;

		public Balance(Money initial)
		{
			MoneyTrail.Add(initial);
			SetKnownBalance(initial);
			Currency = initial.Currency;
		}

		public Currency Currency { get; set; }

		public Money Current
		{
			get
			{
				if (_knownBalance != null)
				{
					return _knownBalance;
				}

				throw new NotSupportedException();
			}
		}

		public List<Money> MoneyTrail { get; set; } = new List<Money>();

		public static Balance Empty(Currency currency)
		{
			return new Balance(new Money(currency, 0, DateTime.MinValue));
		}

		public void Calculate(ICollection<Activity> newSet)
		{
			MoneyTrail.Clear();

			foreach (var activity in newSet.OrderBy(x => x.Date))
			{
				switch (activity.ActivityType)
				{
					case ActivityType.CashDeposit:
					case ActivityType.Dividend:
					case ActivityType.Interest:
					case ActivityType.Sell:
						MoneyTrail.Add(CalculateActivityTotal(activity));
						break;
					case ActivityType.CashWithdrawel:
					case ActivityType.Buy:

						MoneyTrail.Add(CalculateActivityTotal(activity).Negate());
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
					MoneyTrail.Add(activity.Fee.Negate());
				}
			}

			static Money CalculateActivityTotal(Activity activity)
			{
				return new Money(activity.UnitPrice.Currency, activity.UnitPrice.Amount * activity.Quantity, activity.UnitPrice.TimeOfRecord);
			}
		}

		public void SetKnownBalance(Money money)
		{
			lock (this)
			{
				bool isNewer = _knownBalance == null || money.TimeOfRecord > _knownBalance?.TimeOfRecord;
				var isSameDateButLowerAmount = money.TimeOfRecord == _knownBalance?.TimeOfRecord && money.Amount < _knownBalance.Amount;
				if (isNewer || isSameDateButLowerAmount)
				{
					_knownBalance = money;
				}
			}
		}
	}
}