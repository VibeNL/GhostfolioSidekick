using System;
using System.Collections.Generic;
using System.Text;

namespace GhostfolioSidekick.ExternalDataProvider.Cache
{
	public class CacheKey
	{
		public Source Source { get; private set; }
		public TypeOfData DataType { get; private set; }
		public string Key { get; private set; } = string.Empty;
		public DateOnly StartTime { get; private set; }
		public DateOnly EndTime { get; private set; }

		private CacheKey() { }

		public static CacheKey CreateSymbolProfile(Source source, string key)
		{
			return new CacheKey
			{
				Source = source,
				DataType = TypeOfData.SymbolProfile,
				Key = key
			};
		}

		public static CacheKey CreateDividend(Source source, string key)
		{
			return new CacheKey
			{
				Source = source,
				DataType = TypeOfData.Dividends,
				Key = key
			};
		}

		public static CacheKey CreateMarketData(Source source, DateOnly startTime, DateOnly endTime, string key)
		{
			return new CacheKey
			{
				Source = source,
				DataType = TypeOfData.MarketData,
				StartTime = startTime,
				EndTime = endTime,
				Key = key
			};
		}

		internal string GetCombinedKey()
		{
			switch (DataType)
			{
				case TypeOfData.MarketData:
					return $"{Source}:{DataType}:{StartTime:yyyy-MM-dd}:{EndTime:yyyy-MM-dd}:{Key}";
				default:
					return $"{Source}:{DataType}:{Key}";
			}
		}
	}
}
