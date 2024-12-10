using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model
{
	[SuppressMessage("Critical Code Smell", "S2223:Non-constant static fields should not be visible", Justification = "<Pending>")]
	[SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible", Justification = "<Pending>")]
	[SuppressMessage("Minor Code Smell", "S1104:Fields should not have public accessibility", Justification = "<Pending>")]
	public record Currency
	{
		public static Currency EUR = new("EUR");
		public static Currency USD = new("USD");
		public static Currency GBP = new("GBP");
		public static Currency GBp = new("GBp", GBP, 100);
		public static Currency GBX = new("GBX", GBP, 100);

		private static readonly List<Currency> knownCurrencies = [USD, EUR, GBP, GBp, GBX];
				
		public Currency() // EF Core
		{
			Symbol = default!;
		}

		public Currency(string symbol)
		{
			Symbol = symbol;
		}

		public Currency(string symbol, Currency sourceCurrency, decimal factor)
		{
			Symbol = symbol;
			SourceCurrency = sourceCurrency;
			Factor = factor;
		}

		public string Symbol { get; init; }

		public Currency? SourceCurrency { get; init; }

		public decimal Factor { get; init; }

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

			if (SourceCurrency == targetCurrency)
			{
				return Factor;
			}

			if (targetCurrency.SourceCurrency == this)
			{
				return 1 / targetCurrency.Factor;
			}

			return 0;

		}

		public Currency GetSourceCurrency()
		{
			return SourceCurrency != null ? SourceCurrency.GetSourceCurrency() : this;
		}

		public static Currency MapToStatics(Currency sourceCurrency)
		{
			foreach (var currency in knownCurrencies)
			{
				if (currency.Symbol == sourceCurrency.Symbol)
				{
					return currency;
				}
			}

			return sourceCurrency;
		}
	}
}