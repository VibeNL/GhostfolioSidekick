using GhostfolioSidekick.Model.Market;

namespace GhostfolioSidekick.Model.Symbols
{
	public record CurrencyExchangeProfile
	{
		public CurrencyExchangeProfile()
		{
			// EF Core constructor
			SourceCurrency = Currency.USD;
			TargetCurrency = Currency.USD;
		}

		public CurrencyExchangeProfile(Currency sourceCurrency, Currency targetCurrency)
		{
			SourceCurrency = sourceCurrency;
			TargetCurrency = targetCurrency;
		}

		public long ID { get; set; }

		public virtual List<CurrencyExchangeRate> Rates { get; set; } = [];

		public Currency SourceCurrency { get; set; }

		public Currency TargetCurrency { get; set; }
	}
}
