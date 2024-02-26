using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Compare;

namespace GhostfolioSidekick.Model.Activities.Types
{
	public abstract record BaseActivity : IActivity
	{
		public abstract Account Account { get; }
		public abstract DateTime Date { get; }
		public abstract string? TransactionId { get; set; }
		public abstract int? SortingPriority { get; }
		public abstract string? Id { get; set; }

		public abstract Task<bool> AreEqual(IExchangeRateService exchangeRateService, IActivity other);

		protected static bool AreEquals(decimal a, decimal b)
		{
			return Math.Abs(a - b) < Constants.Epsilon;
		}

		protected static bool AreEquals(IExchangeRateService exchangeRateService, Currency target, DateTime dateTime, List<Money> money1, List<Money> money2)
		{
			return AreEquals(money1.Sum(x =>
			{
				var rate = exchangeRateService.GetConversionRate(x.Currency, target, dateTime).Result;
				return rate * x.Amount;
			}), money2.Sum(x =>
			{
				var rate = exchangeRateService.GetConversionRate(x.Currency, target, dateTime).Result;
				return rate * x.Amount;
			}));
		}

		protected static async Task<Money?> RoundAndConvert(IExchangeRateService exchangeRateService, Money value, Currency target, DateTime dateTime)
		{
			static decimal Round(decimal? value)
			{
				var r = Math.Round(value ?? 0, 10);
				return r;
			}

			var rate = await exchangeRateService.GetConversionRate(value.Currency, target, dateTime);
			return new Money(target, Round(value.Amount * rate));
		}
	}
}
