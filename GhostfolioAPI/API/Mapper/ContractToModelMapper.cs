﻿using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Accounts;
using GhostfolioSidekick.Model.Activities;
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
				Utilities.ParseEnum<Datasource>(symbolProfile.DataSource),
				Utilities.ParseEnum<AssetClass>(symbolProfile.AssetClass),
				Utilities.ParseOptionalEnum<AssetSubClass>(symbolProfile.AssetSubClass))
			{
				Comment = symbolProfile.Comment,
				ISIN = symbolProfile.ISIN,
			};
		}

		public static MarketDataProfile MapMarketDataList(Contract.MarketDataList market)
		{
			string? trackinsight = null;
			market.AssetProfile.SymbolMapping?.TryGetValue("TRACKINSIGHT", out trackinsight);
			var assetProfile = market.AssetProfile;
			var mdl = new MarketDataProfile()
			{
				AssetProfile = new SymbolProfile(
					assetProfile.Symbol,
					assetProfile.Name,
					new Currency(assetProfile.Currency),
					Utilities.ParseEnum<Datasource>(assetProfile.DataSource),
					Utilities.ParseEnum<AssetClass>(assetProfile.AssetClass),
					Utilities.ParseOptionalEnum<AssetSubClass>(assetProfile.AssetSubClass))
				{
					ActivitiesCount = assetProfile.ActivitiesCount,
					ISIN = assetProfile.ISIN,
					Comment = assetProfile.Comment,
				},
				MarketData = market.MarketData.Select(MapMarketData).ToList(),
			};

			mdl.AssetProfile.Mappings.TrackInsight = trackinsight;
			return mdl;
		}

		public static MarketData MapMarketData(Contract.MarketData marketData)
		{
			return new MarketData(marketData.Symbol, Utilities.ParseEnum<Datasource>(marketData.DataSource), marketData.MarketPrice, marketData.Date);
		}
	}
}
