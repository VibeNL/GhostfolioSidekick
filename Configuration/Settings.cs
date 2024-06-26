﻿using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace GhostfolioSidekick.Configuration
{
	[ExcludeFromCodeCoverage]
	public class Settings
	{
		[SetsRequiredMembers]
		public Settings()
		{
		}

		[JsonPropertyName("delete.unused.symbols")]
		public bool DeleteUnusedSymbols { get; set; } = true;

		[JsonPropertyName("dataprovider.preference.order")]
		public required string DataProviderPreference { get; set; } = "YAHOO;COINGECKO";

		[JsonPropertyName("use.dust.currency")]
		public string DustCurrency { get; set; } = "USD";

		[JsonPropertyName("use.dust.threshold")]
		public decimal DustThreshold { get; set; } = 0.0001m;

		[JsonPropertyName("use.crypto.workaround.dust.threshold")]
		public decimal CryptoWorkaroundDustThreshold { get; set; } = 0.001m;

		[JsonPropertyName("use.crypto.workaround.stakereward.add.to.last.buy")]
		public bool CryptoWorkaroundStakeReward { get; set; }

		[JsonPropertyName("use.dividend.workaround.tax.substract.from.amount")]
		public bool SubstractTaxesOnDividendFromDividend { get; set; }

		#region Obsolete

		[JsonPropertyName("use.crypto.workaround.stakereward.as.dividends")]
		public bool CryptoWorkaroundStakeRewardObsolete { get; set; }

		[JsonPropertyName("use.crypto.workaround.dust")]
		public bool CryptoWorkaroundDustObsolete { get; set; }
		
		#endregion
	}
}