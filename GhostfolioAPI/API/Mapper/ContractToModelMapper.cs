﻿using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Activities.Types;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.GhostfolioAPI.API.Mapper
{
	internal static class ContractToModelMapper
	{
		public static Platform MapPlatform(Contract.Platform rawPlatform)
		{
			return new Platform(rawPlatform.Name)
			{
				Id = rawPlatform.Id,
				Url = rawPlatform.Url,
			};
		}

		public static Account MapAccount(Contract.Account rawAccount, Platform? platform)
		{
			return new Account(
				rawAccount.Name,
				new Balance(new Money(new Currency(rawAccount.Currency), rawAccount.Balance)))
			{
				Id = rawAccount.Id,
				Comment = rawAccount.Comment,
				Platform = platform,
			};
		}

		public static SymbolProfile ParseSymbolProfile(Contract.SymbolProfile symbolProfile)
		{
			return new SymbolProfile(
				symbolProfile.Symbol,
				symbolProfile.Name,
				new Currency(symbolProfile.Currency!),
				symbolProfile.DataSource,
				Utilities.ParseAssetClass(symbolProfile.AssetClass),
				Utilities.ParseAssetSubClass(symbolProfile.AssetSubClass),
				ParseCountries(symbolProfile.Countries),
				ParseSectors(symbolProfile.Sectors))
			{
				Comment = symbolProfile.Comment,
				ISIN = symbolProfile.ISIN,
			};
		}

		private static Sector[] ParseSectors(Contract.Sector[] sectors)
		{
			return (sectors ?? []).Select(x => new Sector(x.Name, x.Weight)).ToArray();
		}

		private static Country[] ParseCountries(Contract.Country[] countries)
		{
			return (countries ?? []).Select(x => new Country(x.Name, x.Code, x.Continent, x.Weight)).ToArray();
		}

		public static MarketDataProfile MapMarketDataList(Contract.MarketDataList market)
		{
			string? trackinsight = null;
			market.AssetProfile.SymbolMapping?.TryGetValue("TRACKINSIGHT", out trackinsight);
			var assetProfile = market.AssetProfile;
			var mdl = new MarketDataProfile()
			{
				AssetProfile = MapSymbolProfile(assetProfile),
				MarketData = market.MarketData.Select(x => MapMarketData(new Currency(assetProfile.Currency), x)).ToList(),
			};

			mdl.AssetProfile.Mappings.TrackInsight = trackinsight;
			return mdl;
		}

		public static SymbolProfile MapSymbolProfile(Contract.SymbolProfile assetProfile)
		{
			return new SymbolProfile(
								assetProfile.Symbol,
								assetProfile.Name,
								new Currency(assetProfile.Currency),
								assetProfile.DataSource,
								Utilities.ParseAssetClass(assetProfile.AssetClass),
								Utilities.ParseAssetSubClass(assetProfile.AssetSubClass),
								MapCountries(assetProfile.Countries),
								MapSectors(assetProfile.Sectors))
			{
				ActivitiesCount = assetProfile.ActivitiesCount,
				ISIN = assetProfile.ISIN,
				Comment = assetProfile.Comment,
				ScraperConfiguration = new ScraperConfiguration
				{
					Locale = assetProfile.ScraperConfiguration?.Locale,
					Url = assetProfile.ScraperConfiguration?.Url,
					Selector = assetProfile.ScraperConfiguration?.Selector
				}
			};
		}

		private static Sector[] MapSectors(Contract.Sector[] sectors)
		{
			return (sectors ?? []).Select(x => new Sector(x.Name, x.Weight)).ToArray();
		}

		private static Country[] MapCountries(Contract.Country[] countries)
		{
			return (countries ?? []).Select(x => new Country(x.Name, x.Code, x.Continent, x.Weight)).ToArray();
		}

		private static MarketData MapMarketData(Currency currency, Contract.MarketData marketData)
		{
			return new MarketData(new Money(currency, marketData.MarketPrice), marketData.Date);
		}

		internal static IEnumerable<Holding> MapToHoldings(Account[] accounts, Contract.Activity[] existingActivities)
		{
			var dict = new List<Holding>();

			foreach (var activity in existingActivities)
			{
				var profile = activity.SymbolProfile != null ? MapSymbolProfile(activity.SymbolProfile!) : null;

				if (IsAutoGenerated(profile))
				{
					profile = null;
				}

				var holding = dict.SingleOrDefault(x => (profile == null && x.SymbolProfile == null) || (x.SymbolProfile?.Equals(profile) ?? false));
				if (holding == null)
				{
					holding = new Holding(profile);
					dict.Add(holding);
				}

				holding.Activities.Add(MapActivity(accounts, activity));
			}

			return dict;
		}

		private static bool IsAutoGenerated(SymbolProfile? profile)
		{
			if (profile == null)
			{
				return false;
			}

			return
				profile.AssetSubClass == null &&
				profile.AssetClass == AssetClass.Undefined &&
				Datasource.MANUAL.ToString().Equals(profile.DataSource, StringComparison.InvariantCultureIgnoreCase) &&
				Guid.TryParse(profile.Symbol, out _);
		}

		private static IActivity MapActivity(Account[] accounts, Contract.Activity activity)
		{
			switch (activity.Type)
			{
				case Contract.ActivityType.BUY:
				case Contract.ActivityType.SELL:
					return new BuySellActivity(accounts.Single(x => x.Id == activity.AccountId),
								activity.Date,
								(activity.Type == Contract.ActivityType.BUY ? 1 : -1) * activity.Quantity,
								new Money(new Currency(activity.SymbolProfile!.Currency), activity.UnitPrice),
								TransactionReferenceUtilities.ParseComment(activity)
								)
					{
						Fees = new[] { new Money(new Currency(activity.SymbolProfile!.Currency), activity.Fee) },
						Description = activity.SymbolProfile.Name,
						Id = activity.Id,
					};
				case Contract.ActivityType.DIVIDEND:
					return new DividendActivity(accounts.Single(x => x.Id == activity.AccountId),
								activity.Date,
								new Money(new Currency(activity.SymbolProfile!.Currency), activity.Quantity * activity.UnitPrice),
								TransactionReferenceUtilities.ParseComment(activity)
								)
					{
						Fees = new[] { new Money(new Currency(activity.SymbolProfile!.Currency), activity.Fee) },
						Description = activity.SymbolProfile.Name,
						Id = activity.Id,
					};
				case Contract.ActivityType.INTEREST:
					return new InterestActivity(accounts.Single(x => x.Id == activity.AccountId),
								activity.Date,
								new Money(new Currency(activity.SymbolProfile!.Currency), activity.Quantity * activity.UnitPrice),
								TransactionReferenceUtilities.ParseComment(activity)
								)
					{
						Description = activity.SymbolProfile.Name,
						Id = activity.Id,
					};
				case Contract.ActivityType.FEE:
					return new FeeActivity(accounts.Single(x => x.Id == activity.AccountId),
								activity.Date,
								new Money(new Currency(activity.SymbolProfile!.Currency), activity.Quantity * activity.UnitPrice),
								TransactionReferenceUtilities.ParseComment(activity)
								)
					{
						Description = activity.SymbolProfile.Name,
						Id = activity.Id,
					};
				case Contract.ActivityType.ITEM:
					return new ValuableActivity(accounts.Single(x => x.Id == activity.AccountId),
								activity.Date,
								new Money(new Currency(activity.SymbolProfile!.Currency), activity.Quantity * activity.UnitPrice),
								TransactionReferenceUtilities.ParseComment(activity)
								)
					{
						Description = activity.SymbolProfile.Name,
						Id = activity.Id,
					};
				case Contract.ActivityType.LIABILITY:
					return new LiabilityActivity(accounts.Single(x => x.Id == activity.AccountId),
								activity.Date,
								new Money(new Currency(activity.SymbolProfile!.Currency), activity.Quantity * activity.UnitPrice),
								TransactionReferenceUtilities.ParseComment(activity)
								)
					{
						Description = activity.SymbolProfile.Name,
						Id = activity.Id,
					};
				default:
					throw new NotSupportedException($"ActivityType {activity.Type} not supported");
			}
		}
	}
}
