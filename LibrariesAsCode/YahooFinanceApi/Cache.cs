using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace YahooFinanceApi;

public static class Cache
{
    private static Dictionary<string, TimeZoneInfo> timeZoneCache = [];

    public static async Task<TimeZoneInfo> GetTimeZone(string ticker)
    {
        if (timeZoneCache.TryGetValue(ticker, out var zone))
            return zone;

        var timeZone = await RequestTimeZone(ticker);
        timeZoneCache[ticker] = timeZone;
        return timeZone;
    }

    private static async Task<TimeZoneInfo> RequestTimeZone(string ticker)
    {
        var startTime = DateTime.Now.AddDays(-2);
        var endTime = DateTime.Now;
        var data = await ChartDataLoader.GetResponseStreamAsync(ticker, startTime, endTime, Period.Daily, ShowOption.History.Name(), CancellationToken.None);
        var timeZoneName = data.chart.result[0].meta.exchangeTimezoneName;
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(timeZoneName);
        }
        catch (TimeZoneNotFoundException e)
        {
            return TimeZoneInfo.Utc;
        }
    }
}