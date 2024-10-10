using CsvHelper;
using Flurl;
using Flurl.Http;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Diagnostics;
using System.Dynamic;
using System.Globalization;
using System.Linq;

namespace YahooFinanceApi;

public sealed partial class Yahoo
{
    public static CultureInfo Culture = CultureInfo.InvariantCulture;
    public static bool IgnoreEmptyRows { set { DataConvertors.IgnoreEmptyRows = value; } }

    public static async Task<IReadOnlyList<Candle>> GetHistoricalAsync(string symbol, DateTime? startTime = null, DateTime? endTime = null, Period period = Period.Daily, CancellationToken token = default)
        => await GetTicksAsync(symbol, 
            startTime, 
            endTime, 
            period, 
            ShowOption.History,
            DataConvertors.ToCandle,
            token).ConfigureAwait(false);

    public static async Task<IReadOnlyList<DividendTick>> GetDividendsAsync(string symbol, DateTime? startTime = null, DateTime? endTime = null, CancellationToken token = default)
        => await GetTicksAsync(symbol, 
            startTime, 
            endTime, 
            Period.Daily, 
            ShowOption.Dividend,
            DataConvertors.ToDividendTick,
            token).ConfigureAwait(false);

    public static async Task<IReadOnlyList<SplitTick>> GetSplitsAsync(string symbol, DateTime? startTime = null, DateTime? endTime = null, CancellationToken token = default)
        => await GetTicksAsync(symbol,
            startTime,
            endTime,
            Period.Daily,
            ShowOption.Split,
            DataConvertors.ToSplitTick,
            token).ConfigureAwait(false);

    private static async Task<List<T>> GetTicksAsync<T>
    (
        string symbol,
        DateTime? startTime,
        DateTime? endTime,
        Period period,
        ShowOption showOption,
        Func<ExpandoObject, TimeZoneInfo, List<T>> converter,
        CancellationToken token
    )
        where T : ITick
    {
        await YahooSession.InitAsync(token);
        TimeZoneInfo symbolTimeZone = await Cache.GetTimeZone(symbol);

        startTime ??= Helper.Epoch;
        endTime ??= DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Utc);
        DateTime start = startTime.Value.ToUtcFrom(symbolTimeZone);
        DateTime end = endTime.Value.AddDays(2).ToUtcFrom(symbolTimeZone);
        
        dynamic json = await GetResponseStreamAsync(symbol, start, end, period, showOption.Name(), token).ConfigureAwait(false);
        dynamic data = json.chart.result[0];

        List<T> allData = converter(data, symbolTimeZone);
        return allData.Where(x => x != null).Where(x => x.DateTime <= endTime.Value).ToList();
    }

    private static async Task<dynamic> GetResponseStreamAsync(string symbol, DateTime startTime, DateTime endTime, Period period, string events, CancellationToken token)
    {
        bool reset = false;
        while (true)
        {
            try
            {
                return await ChartDataLoader.GetResponseStreamAsync(symbol, startTime, endTime, period, events, token).ConfigureAwait(false);
            }
            catch (FlurlHttpException ex) when (ex.Call.Response?.StatusCode == (int)HttpStatusCode.NotFound)
            {
                throw new Exception($"Invalid ticker or endpoint for symbol '{symbol}'.", ex);
            }
            catch (FlurlHttpException ex) when (ex.Call.Response?.StatusCode == (int)HttpStatusCode.Unauthorized)
            {
                Debug.WriteLine("GetResponseStreamAsync: Unauthorized.");

                if (reset)
                    throw;
                reset = true; // try again with a new client
            }
        }
    }
}