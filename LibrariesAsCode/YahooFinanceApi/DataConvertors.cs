using System;
using System.Collections.Generic;
using System.Linq;

namespace YahooFinanceApi
{
    internal static class DataConvertors
    {
        internal static bool IgnoreEmptyRows;

        internal static List<Candle> ToCandle(dynamic data, TimeZoneInfo timeZone)
        {
            List<object> timestamps = data.timestamp;
            DateTime[] dates = timestamps.Select(x => x.ToDateTime(timeZone).Date).ToArray();
            IDictionary<string, object> indicators = data.indicators;
            IDictionary<string, object> values = data.indicators.quote[0];

            if (indicators.ContainsKey("adjclose"))
                values["adjclose"] = data.indicators.adjclose[0].adjclose;

            var ticks = new List<Candle>();
            
            for (int i = 0; i < dates.Length; i++)
            {
                var slice = new Dictionary<string, object>();
                foreach (KeyValuePair<string, object> pair in values)
                {
                    List<object> ts = (List<object>) pair.Value;
                    slice.Add(pair.Key, ts[i]);
                }
                ticks.Add(CreateCandle(dates[i], slice));
            }

            return ticks;
            
            Candle CreateCandle(DateTime date, IDictionary<string, object> row)
            {
                var candle = new Candle
                {
                    DateTime      = date,
                    Open          = row.GetValueOrDefault("open").ToDecimal(),
                    High          = row.GetValueOrDefault("high").ToDecimal(),
                    Low           = row.GetValueOrDefault("low").ToDecimal(),
                    Close         = row.GetValueOrDefault("close").ToDecimal(),
                    AdjustedClose = row.GetValueOrDefault("adjclose").ToDecimal(),
                    Volume        = row.GetValueOrDefault("volume").ToInt64()
                };

                if (IgnoreEmptyRows &&
                    candle.Open == 0 && candle.High == 0 && candle.Low == 0 && candle.Close == 0 &&
                    candle.AdjustedClose == 0 &&  candle.Volume == 0)
                    return null;

                return candle;
            }
        }

        internal static List<DividendTick> ToDividendTick(dynamic data, TimeZoneInfo timeZone)
        {
            IDictionary<string, object> expandoObject = data;

            if (!expandoObject.ContainsKey("events"))
                return new List<DividendTick>();
            
            IDictionary<string, dynamic> dvdObj = data.events.dividends;
            var dividends = dvdObj.Values.Select(x => new DividendTick(ToDateTime(x.date, timeZone), ToDecimal(x.amount))).ToList();

            if (IgnoreEmptyRows)
                dividends = dividends.Where(x => x.Dividend > 0).ToList();

            return dividends;
        }

        internal static List<SplitTick> ToSplitTick(dynamic data, TimeZoneInfo timeZone)
        {
            IDictionary<string, dynamic> splitsObj = data.events.splits;
            var splits = splitsObj.Values.Select(x => new SplitTick(ToDateTime(x.date, timeZone), ToDecimal(x.numerator), ToDecimal(x.denominator))).ToList();
            
            if (IgnoreEmptyRows)
                splits = splits.Where(x => x.BeforeSplit > 0 && x.AfterSplit > 0).ToList();
            
            return splits;
        }

        private static DateTime ToDateTime(this object obj, TimeZoneInfo timeZone)
        {
            if (obj is long lng)
            {
                return TimeZoneInfo.ConvertTimeFromUtc(DateTimeOffset.FromUnixTimeSeconds(lng).DateTime, timeZone);
            }

            throw new Exception($"Could not convert '{obj}' to DateTime.");
        }
    
        private static Decimal ToDecimal(this object obj)
        {
            return Convert.ToDecimal(obj);
        }

        private static Int64 ToInt64(this object obj)
        {
            return Convert.ToInt64(obj);
        }
    }
}
