﻿using GhostfolioSidekick.Crypto;
using GhostfolioSidekick.Ghostfolio.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.FileImporter
{
	public abstract class CryptoRecordBaseImporter<T> : RecordBaseImporter<T>
	{
		protected CryptoRecordBaseImporter(IGhostfolioAPI api) : base(api)
		{
		}

		protected async Task<decimal> GetCorrectUnitPrice(decimal? originalUnitPrice, Asset? symbol, DateTime date)
		{
			if (originalUnitPrice > 0)
			{
				return originalUnitPrice.Value;
			}

			// GetPrice from Ghostfolio
			var price = await api.GetMarketPrice(symbol, date);
			return price;
		}

		protected Asset? ParseFindSymbolByISINResult(string assetName, string symbol, IEnumerable<Asset> assets)
		{
			var cryptoOnly = assets.Where(x => x.AssetSubClass == "CRYPTOCURRENCY");
			var asset = cryptoOnly.FirstOrDefault(x => assetName == x.Name);
			if (asset != null)
			{
				return asset;
			}

			asset = cryptoOnly.FirstOrDefault(x => symbol == x.Symbol);
			if (asset != null)
			{
				return asset;
			}

			asset = cryptoOnly
				.OrderBy(x => x.Symbol.Length)
				.FirstOrDefault();

			return asset;
		}

		protected OrderType? Convert(CryptoOrderType? value)
		{
			if (value is null)
			{
				return null;
			}

			return value switch
			{
				CryptoOrderType.Buy => (OrderType?)OrderType.BUY,
				CryptoOrderType.Sell => (OrderType?)OrderType.SELL,
				CryptoOrderType.Send => (OrderType?)OrderType.SELL,
				CryptoOrderType.Receive => (OrderType?)OrderType.BUY,
				CryptoOrderType.Convert => (OrderType?)OrderType.SELL,
				CryptoOrderType.LearningReward or CryptoOrderType.StakingReward => null,
				_ => throw new NotSupportedException(),
			};
		}
	}
}
