using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Flurl;
using Flurl.Http;

namespace YahooFinanceApi;

public static class ChartDataLoader
{
    public static async Task<dynamic> GetResponseStreamAsync(string symbol, DateTime startTime, DateTime endTime, Period period, string events, CancellationToken token)
    {
        var url = "https://query2.finance.yahoo.com/v8/finance/chart/"
            .AppendPathSegment(symbol)
            .SetQueryParam("period1", startTime.ToUnixTimestamp())
            .SetQueryParam("period2", endTime.ToUnixTimestamp())
            .SetQueryParam("interval", $"1{period.Name()}")
            .SetQueryParam("events", events)
            .SetQueryParam("crumb", YahooSession.Crumb);

        Debug.WriteLine(url);

        var response = await url
            .WithCookie(YahooSession.Cookie.Name, YahooSession.Cookie.Value)
            .WithHeader(YahooSession.UserAgentKey, YahooSession.UserAgentValue)
            // .AllowHttpStatus("500")
            .GetAsync(token);

        var json = await response.GetJsonAsync();

        var error = json.chart?.error?.description;
        if (error != null)
        {
            throw new InvalidDataException($"An error was returned by Yahoo: {error}");
        }
                
        return json;
    }
}