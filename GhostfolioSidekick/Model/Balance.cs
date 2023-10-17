namespace GhostfolioSidekick.Model
{
	public class Balance
	{
		private volatile Money? _knownBalance;

		public Balance(Money initial)
		{
			MoneyTrail.Add(initial);
			SetKnownBalance(initial);
			Currency = initial.Currency;
		}

		public Currency Currency { get; set; }

		public Money Current(ICurrentPriceCalculator currentPriceCalculator)
		{
			if (_knownBalance != null)
			{
				return _knownBalance;
			}

			var targetCurrency = Currency;
			decimal amount = 0;
			foreach (var item in MoneyTrail)
			{
				if (item.Currency.Equals(targetCurrency))
				{
					amount += item.Amount;
				}
				else
				{
					amount += currentPriceCalculator.GetConvertedPrice(item, targetCurrency, item.TimeOfRecord)?.Amount ?? 0;
				}
			}

			return new Money(targetCurrency, amount, MoneyTrail.Select(x => x.TimeOfRecord).DefaultIfEmpty().Max());
		}

		private List<Money> MoneyTrail { get; set; } = new List<Money>();

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
					case ActivityType.CashWithdrawal:
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

		internal void Empty()
		{
			lock (this)
			{
				_knownBalance = null;
				MoneyTrail.Clear();
			}
		}
	}
}