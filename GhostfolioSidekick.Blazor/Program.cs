using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using GhostfolioSidekick.Blazor;
using GhostfolioSidekick;
using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.Database;
using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.GhostfolioAPI;
using GhostfolioSidekick.GhostfolioAPI.API;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using RestSharp;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddSingleton<MemoryCache, MemoryCache>();
builder.Services.AddSingleton<IMemoryCache>(x => x.GetRequiredService<MemoryCache>());
builder.Services.AddSingleton<IApplicationSettings, ApplicationSettings>();

builder.Services.AddSingleton<IRestClient, RestClient>(x =>
{
    var settings = x.GetService<IApplicationSettings>();
    var options = new RestClientOptions(settings!.GhostfolioUrl)
    {
        ThrowOnAnyError = false,
        ThrowOnDeserializationError = false,
    };

    return new RestClient(options);
});

builder.Services.AddSingleton(x =>
{
    var settings = x.GetService<IApplicationSettings>();
    return new RestCall(x.GetService<IRestClient>()!,
                        x.GetService<MemoryCache>()!,
                        x.GetService<ILogger<RestCall>>()!,
                        settings!.GhostfolioUrl,
                        settings!.GhostfolioAccessToken,
                        new RestCallOptions() { TrottleTimeout = TimeSpan.FromSeconds(settings!.TrottleTimeout) });
});
builder.Services.AddSingleton(x =>
{
    var settings = x.GetService<IApplicationSettings>();
    return settings!.ConfigurationInstance.Settings;
});
builder.Services.AddDbContextFactory<DatabaseContext>(options =>
{
    var settings = builder.Services.BuildServiceProvider().GetService<IApplicationSettings>();
    options.UseSqlite($"Data Source={settings!.FileImporterPath}/ghostfoliosidekick.db");
});

builder.Services.AddSingleton<ICurrencyMapper, SymbolMapper>();
builder.Services.AddSingleton<ICurrencyExchange, CurrencyExchange>();
builder.Services.AddSingleton<IApiWrapper, ApiWrapper>();

builder.Services.AddSingleton<YahooRepository>();
builder.Services.AddSingleton<CoinGeckoRepository>();
builder.Services.AddSingleton<GhostfolioSymbolMatcher>();
builder.Services.AddSingleton<ManualSymbolMatcher>();
builder.Services.AddTransient<ICoinGeckoRestClient, CoinGeckoRestClient>();

builder.Services.AddSingleton<ICurrencyRepository>(sp => sp.GetRequiredService<YahooRepository>());
builder.Services.AddSingleton<ISymbolMatcher[]>(sp => [
        sp.GetRequiredService<YahooRepository>(), 
        sp.GetRequiredService<CoinGeckoRepository>(),
        sp.GetRequiredService<GhostfolioSymbolMatcher>(),
        sp.GetRequiredService<ManualSymbolMatcher>()
    ]);
builder.Services.AddSingleton<IStockPriceRepository[]>(sp => [sp.GetRequiredService<YahooRepository>(), sp.GetRequiredService<CoinGeckoRepository>()]);
builder.Services.AddSingleton<IStockSplitRepository[]>(sp => [sp.GetRequiredService<YahooRepository>()]);
builder.Services.AddSingleton<IGhostfolioSync, GhostfolioSync>();
builder.Services.AddSingleton<IGhostfolioMarketData, GhostfolioMarketData>();

builder.Services.AddScoped<IHostedService, TimedHostedService>();
RegisterAllWithInterface<IScheduledWork>(builder.Services);
RegisterAllWithInterface<IHoldingStrategy>(builder.Services);
RegisterAllWithInterface<IFileImporter>(builder.Services);

builder.Services.AddScoped<IPdfToWordsParser, PdfToWordsParser>();

await builder.Build().RunAsync();

void RegisterAllWithInterface<T>(IServiceCollection services)
{
    var types = typeof(T).Assembly.GetTypes()
        .Where(t => t.GetInterfaces().Contains(typeof(T)) && !t.IsInterface && !t.IsAbstract);
    foreach (var type in types)
    {
        services.AddScoped(typeof(T), type);
    }
}
