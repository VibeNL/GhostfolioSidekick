using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace GhostfolioSidekick.Model
{
	[SuppressMessage("Critical Code Smell", "S2223:Non-constant static fields should not be visible", Justification = "<Pending>")]
	[SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible", Justification = "<Pending>")]
	[SuppressMessage("Minor Code Smell", "S1104:Fields should not have public accessibility", Justification = "<Pending>")]
	public record Currency
	{
		public static readonly Currency EUR = new("EUR");
		public static readonly Currency USD = new("USD");
		public static readonly Currency GBP = new("GBP");
		public static readonly Currency GBp = new("GBp");
		public static readonly Currency GBX = new("GBX");

		private static readonly List<Currency> knownCurrencies = [EUR, USD, GBP, GBp, GBX];
		private static readonly List<Currency> allCurrencies = [.. knownCurrencies];

		private static readonly List<Tuple<Currency, Currency, decimal>> knownExchangeRates =
		[
			Tuple.Create(GBp, GBP, 0.01m),
			Tuple.Create(GBX, GBP, 0.01m),
		];

		public Currency() // EF Core
		{
			Symbol = default!;
		}

		public static Currency GetCurrency(string symbol)
		{
			var currency = allCurrencies.FirstOrDefault(c => c.Symbol == symbol);
			if (currency != null)
			{
				return currency;
			}

			Currency newCurrency = new(symbol);
			allCurrencies.Add(newCurrency);
			return newCurrency;
		}

		private Currency(string symbol)
		{
			Symbol = symbol;
		}

		public string Symbol { get; init; }

		public bool IsFiat()
		{
			return knownCurrencies.Exists(x => x.Symbol == Symbol);
		}

		public override string ToString()
		{
			return Symbol;
		}

		public decimal GetKnownExchangeRate(Currency targetCurrency)
		{
			if (this == targetCurrency)
			{
				return 1;
			}

			foreach (var pair in knownExchangeRates)
			{
				if (pair.Item1 == this && pair.Item2 == targetCurrency)
				{
					return pair.Item3;
				}

				if (pair.Item1 == targetCurrency && pair.Item2 == this)
				{
					return 1 / pair.Item3;
				}
			}

			return 0;

		}

		public (Currency, decimal) GetSourceCurrency()
		{
			foreach (var pair in knownExchangeRates)
			{
				if (pair.Item1 == this)
				{
					return (pair.Item2, pair.Item3);
				}
			}

			return (this, 1);
		}
	}
}