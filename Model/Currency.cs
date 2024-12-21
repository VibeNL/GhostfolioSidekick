using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.Model
{
	[SuppressMessage("Critical Code Smell", "S2223:Non-constant static fields should not be visible", Justification = "<Pending>")]
	[SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible", Justification = "<Pending>")]
	[SuppressMessage("Minor Code Smell", "S1104:Fields should not have public accessibility", Justification = "<Pending>")]
	public record Currency
	{
		public static Currency EUR = new("EUR", null, 0);
		public static Currency USD = new("USD", null, 0);
		public static Currency GBP = new("GBP", null, 0);
		public static Currency GBp = new("GBp", GBP, 100);
		public static Currency GBX = new("GBX", GBP, 100);

		private static readonly List<Currency> knownCurrencies = [USD, EUR, GBP, GBp, GBX];
				
		public Currency() // EF Core
		{
			Symbol = default!;
		}

		public static Currency GetCurrency(string symbol)
		{
			foreach (var currency in knownCurrencies)
			{
				if (currency.Symbol == symbol)
				{
					// Create a copy due to EF Core
					return currency with { };
				}
			}

			return new Currency(symbol, null!, 0);
		}

		private Currency(string symbol, Currency? sourceCurrency, decimal factor)
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
				return 1 / Factor;
			}

			if (targetCurrency.SourceCurrency == this)
			{
				return targetCurrency.Factor;
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