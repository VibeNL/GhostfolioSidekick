using GhostfolioSidekick.Model.Compare;

namespace GhostfolioSidekick.Model
{
	public static class CompareUtilities
	{
		public static bool AreNumbersEquals(decimal? a, decimal? b)
		{
			return Math.Abs((a ?? 0) - (b ?? 0)) < Constants.Epsilon;
		}

		public static bool AreNumbersEquals(Money? a, Money? b)
		{
			return Math.Abs((a?.Amount ?? 0) - (b?.Amount ?? 0)) < Constants.Epsilon;
		}

		public static bool AreMoneyEquals(IExchangeRateService exchangeRateService, Currency? target, DateTime dateTime, List<Money> money1, List<Money> money2)
		{
			if (target == null)
			{
				return false;
			}

			return AreNumbersEquals(money1.Sum(x =>
			{
				var rate = exchangeRateService.GetConversionRate(x.Currency, target, dateTime).Result;
				return rate * x.Amount;
			}), money2.Sum(x =>
			{
				var rate = exchangeRateService.GetConversionRate(x.Currency, target, dateTime).Result;
				return rate * x.Amount;
			}));
		}

		public static async Task<Money?> RoundAndConvert(IExchangeRateService exchangeRateService, Money? value, Currency? target, DateTime dateTime)
		{
			if (target == null || value == null)
			{
				return value;
			}

			static decimal Round(decimal? value)
			{
				var r = Math.Round(value ?? 0, 10);
				return r;
			}

			var rate = await exchangeRateService.GetConversionRate(value!.Currency, target, dateTime);
			return new Money(target, Round(value.Amount * rate));
		}
	}
}
