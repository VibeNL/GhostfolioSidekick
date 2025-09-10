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
			var symbol = symbols.FirstOrDefault(s => s.Symbol == rawActivity.SymbolProfile.Symbol);
			if (symbol == null)
			{
				throw new ArgumentException($"Symbol {rawActivity.SymbolProfile} not found.");
			}

			return rawActivity.Type switch
			{
				Contract.ActivityType.BUY => new BuyActivity(),
				Contract.ActivityType.SELL => new SellActivity(),
				Contract.ActivityType.DIVIDEND => new DividendActivity(),
				Contract.ActivityType.INTEREST => new InterestActivity(),
				Contract.ActivityType.FEE => new FeeActivity(),
				Contract.ActivityType.ITEM => new ValuableActivity(),
				Contract.ActivityType.LIABILITY => new LiabilityActivity(),
				_ => throw new NotSupportedException(),
			};
		}
	}
}
