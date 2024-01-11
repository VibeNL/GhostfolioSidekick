﻿using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.Model;

namespace GhostfolioSidekick.FileImporter
{
	public abstract class CryptoRecordBaseImporter<T> : RecordBaseImporter<T>
	{
		private readonly Settings settings;

		protected CryptoRecordBaseImporter(
			ConfigurationInstance configurationInstance,
			IGhostfolioAPI api) : base(api)
		{
			settings = configurationInstance.Settings;
		}

		protected async Task<Money> GetCorrectUnitPrice(Money originalUnitPrice, SymbolProfile symbol, DateTime date)
		{
			if (originalUnitPrice.Amount > 0)
			{
				return originalUnitPrice;
			}

			// GetPrice from Ghostfolio
			var price = await api.GetMarketPrice(symbol, date);
			return price ?? new Money(symbol.Currency, 0, date);
		}

		protected async Task<SymbolProfile?> GetAsset(string assetName, Currency defaultCurrency)
		{
			var mappedName = CryptoMapper.Instance.GetFullname(assetName);

			return await api.FindSymbolByIdentifier(
				new[] { mappedName, assetName },
				defaultCurrency,
				DefaultSetsOfAssetClasses.CryptoBrokerDefaultSetAssestClasses,
				DefaultSetsOfAssetClasses.CryptoBrokerDefaultSetAssetSubClasses);
		}
	}
}
