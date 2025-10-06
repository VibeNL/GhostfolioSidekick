using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.GhostfolioAPI.API.Mapper
{
	public static class ContractToModelMapper
	{
		public static Platform MapPlatform(Contract.Platform rawPlatform)
		{
			return new Platform(rawPlatform.Name)
			{
				Url = rawPlatform.Url,
			};
		}

		public static Account MapAccount(Contract.Account rawAccount, Contract.Platform? platform)
		{
			var account = new Account(rawAccount.Name)
			{
				Comment = rawAccount.Comment,
				Platform = platform != null ? MapPlatform(platform) : null,
			};

			account.Balance = [new Balance(DateOnly.FromDateTime(DateTime.Today), new Money(Currency.GetCurrency(rawAccount.Currency), rawAccount.Balance))];

			return account;
		}
		public static SymbolProfile MapSymbolProfile(Contract.SymbolProfile symbolProfile)
		{
			var symbol = new SymbolProfile(
				symbolProfile.Symbol,
				symbolProfile.Name,
				MapIdentifiers(symbolProfile),
				Currency.GetCurrency(symbolProfile.Currency!),
				Datasource.GHOSTFOLIO + "_" + symbolProfile.DataSource,
				EnumMapper.ParseAssetClass(symbolProfile.AssetClass),
				EnumMapper.ParseAssetSubClass(symbolProfile.AssetSubClass),
				MapCountries(symbolProfile.Countries),
				MapSectors(symbolProfile.Sectors))
			{
				Comment = symbolProfile.Comment,
				ISIN = symbolProfile.ISIN,
			};

			return symbol;
		}

		private static List<string> MapIdentifiers(Contract.SymbolProfile symbolProfile)
		{
			List<string> value = [symbolProfile.ISIN, symbolProfile.Symbol];
			value = [.. value.Where(x => !string.IsNullOrWhiteSpace(x))];
			return value;
		}

		private static SectorWeight[] MapSectors(Contract.Sector[] sectors)
		{
			return [.. (sectors ?? []).Select(x => new SectorWeight(x.Name, x.Weight))];
		}

		private static CountryWeight[] MapCountries(Contract.Country[] countries)
		{
			return [.. (countries ?? []).Select(x => new CountryWeight(x.Name, x.Code, x.Continent, x.Weight))];
		}

		internal static Model.Activities.Activity MapActivity(Account account, ICurrencyExchange currencyExchange, List<Contract.SymbolProfile> symbols, Contract.Activity rawActivity)
		{
			var symbol = symbols.FirstOrDefault(s => s.Symbol == rawActivity.SymbolProfile.Symbol) ?? throw new ArgumentException($"Symbol {rawActivity.SymbolProfile} not found.");
			
			// Create partial symbol identifiers based on the symbol profile
			var partialSymbolIdentifiers = new List<Model.Activities.PartialSymbolIdentifier>
			{
				Model.Activities.PartialSymbolIdentifier.CreateGeneric(symbol.Symbol)
			};

			// Create money objects for amounts
			var currency = Currency.GetCurrency(symbol.Currency);
			var unitPrice = new Money(currency, rawActivity.UnitPrice);
			var feeAmount = rawActivity.Fee > 0 ? new Money(Currency.GetCurrency(rawActivity.FeeCurrency ?? symbol.Currency), rawActivity.Fee) : null;

			return rawActivity.Type switch
			{
				Contract.ActivityType.BUY => new BuyActivity(
					account,
					null, // holding
					partialSymbolIdentifiers,
					rawActivity.Date,
					rawActivity.Quantity,
					unitPrice,
					rawActivity.ReferenceCode ?? rawActivity.Id ?? Guid.NewGuid().ToString(),
					null, // sortingPriority
					rawActivity.Comment)
				{
					TotalTransactionAmount = new Money(currency, rawActivity.Quantity * rawActivity.UnitPrice),
					Fees = feeAmount != null ? [new Model.Activities.Types.MoneyLists.BuyActivityFee(feeAmount)] : []
				},

				Contract.ActivityType.SELL => new SellActivity(
					account,
					null, // holding
					partialSymbolIdentifiers,
					rawActivity.Date,
					rawActivity.Quantity,
					unitPrice,
					rawActivity.ReferenceCode ?? rawActivity.Id ?? Guid.NewGuid().ToString(),
					null, // sortingPriority
					rawActivity.Comment)
				{
					TotalTransactionAmount = new Money(currency, rawActivity.Quantity * rawActivity.UnitPrice),
					Fees = feeAmount != null ? [new Model.Activities.Types.MoneyLists.SellActivityFee(feeAmount)] : []
				},

				Contract.ActivityType.DIVIDEND => new DividendActivity(
					account,
					null, // holding
					partialSymbolIdentifiers,
					rawActivity.Date,
					new Money(currency, rawActivity.UnitPrice), // dividend amount
					rawActivity.ReferenceCode ?? rawActivity.Id ?? Guid.NewGuid().ToString(),
					null, // sortingPriority
					rawActivity.Comment)
				{
					Fees = feeAmount != null ? [new Model.Activities.Types.MoneyLists.DividendActivityFee(feeAmount)] : []
				},

				Contract.ActivityType.INTEREST => new InterestActivity(
					account,
					null, // holding
					rawActivity.Date,
					new Money(currency, rawActivity.UnitPrice), // interest amount
					rawActivity.ReferenceCode ?? rawActivity.Id ?? Guid.NewGuid().ToString(),
					null, // sortingPriority
					rawActivity.Comment),

				Contract.ActivityType.FEE => new FeeActivity(
					account,
					null, // holding
					rawActivity.Date,
					new Money(currency, rawActivity.UnitPrice), // fee amount
					rawActivity.ReferenceCode ?? rawActivity.Id ?? Guid.NewGuid().ToString(),
					null, // sortingPriority
					rawActivity.Comment),

				Contract.ActivityType.ITEM => new ValuableActivity(
					account,
					null, // holding
					partialSymbolIdentifiers,
					rawActivity.Date,
					new Money(currency, rawActivity.UnitPrice), // valuable amount
					rawActivity.ReferenceCode ?? rawActivity.Id ?? Guid.NewGuid().ToString(),
					null, // sortingPriority
					rawActivity.Comment),

				Contract.ActivityType.LIABILITY => new LiabilityActivity(
					account,
					null, // holding
					partialSymbolIdentifiers,
					rawActivity.Date,
					new Money(currency, rawActivity.UnitPrice), // liability amount
					rawActivity.ReferenceCode ?? rawActivity.Id ?? Guid.NewGuid().ToString(),
					null, // sortingPriority
					rawActivity.Comment),

				_ => throw new NotSupportedException($"Activity type {rawActivity.Type} is not supported."),
			};
		}
	}
}
