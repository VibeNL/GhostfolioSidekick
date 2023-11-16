using GhostfolioSidekick.Model;
using System.Collections;

namespace GhostfolioSidekick
{
	internal class CurrencyPair : IEnumerable<Currency>
	{
		public CurrencyPair(Currency a, Currency b, decimal conversionRateAToB)
		{
			A = a;
			B = b;
			ConversionRateAToB = conversionRateAToB;
		}

		public Currency A { get; }
		public Currency B { get; }
		public decimal ConversionRateAToB { get; }

		public decimal CalculateRate(Currency currency, bool inversion)
		{
			if (currency == A)
			{
				return 1;
			}

			return inversion ? 1 / ConversionRateAToB : ConversionRateAToB;
		}

		public IEnumerator<Currency> GetEnumerator()
		{
			return GetList();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetList();
		}

		private IEnumerator<Currency> GetList()
		{
			if (A == B)
			{
				return new[] { A }.ToList().GetEnumerator();
			}

			return new[] { A, B }.ToList().GetEnumerator();
		}
	}
}