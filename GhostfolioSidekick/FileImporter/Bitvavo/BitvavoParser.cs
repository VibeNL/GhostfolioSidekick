﻿using CsvHelper.Configuration;
using GhostfolioSidekick.Ghostfolio.API;
using GhostfolioSidekick.Model;
using System.Globalization;

namespace GhostfolioSidekick.FileImporter.Nexo
{
	public class BitvavoParser : CryptoRecordBaseImporter<BitvavoRecord>
	{
		public BitvavoParser(IGhostfolioAPI api) : base(api)
		{
		}

		protected override async Task<IEnumerable<Activity>> ConvertOrders(BitvavoRecord record, Account account, IEnumerable<BitvavoRecord> allRecords)
		{
			if (record.Status != "Completed")
			{
				return Array.Empty<Activity>();
			}

			var activities = new List<Activity>();

			var asset = await GetAsset(record.Currency);
			DateTime dateTime = record.Date.ToDateTime(record.Time);
			var activity = new Activity
			{
				Asset = asset,
				Date = dateTime,
				Comment = TransactionReferenceUtilities.GetComment(record.Transaction, record.Currency),
				Quantity = Math.Abs(record.Amount),
				ActivityType = MapType(record.Type),
				UnitPrice = new Money(CurrencyHelper.EUR, record.Price ?? 0, dateTime),//TODO
				ReferenceCode = record.Transaction,
			};

			activities.Add(activity);

			return activities;

			async Task<SymbolProfile?> GetAsset(string assetName)
			{
				var mappedName = CryptoMapper.Instance.GetFullname(assetName);

				return await api.FindSymbolByIdentifier(
					mappedName,
					account.Balance.Currency,
					DefaultSetsOfAssetClasses.CryptoBrokerDefaultSetAssestClasses,
					DefaultSetsOfAssetClasses.CryptoBrokerDefaultSetAssetSubClasses);
			}
		}

		private ActivityType MapType(string type)
		{
			switch (type)
			{
				case "buy":
					return ActivityType.Buy;
				case "sell":
					return ActivityType.Sell;
				case "staking":
					return ActivityType.StakingReward;
				case "withdrawal":
					return ActivityType.CashWithdrawal;
				case "deposit":
					return ActivityType.CashDeposit;
				case "rebate":
					return ActivityType.CashDeposit;
				case "affiliate":
					return ActivityType.CashDeposit;
				default:
					throw new NotSupportedException();
			}
		}

		protected override CsvConfiguration GetConfig()
		{
			return new CsvConfiguration(CultureInfo.InvariantCulture)
			{
				HasHeaderRecord = true,
				CacheFields = true,
				Delimiter = ",",
			};
		}
	}
}
