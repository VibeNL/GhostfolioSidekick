using GhostfolioSidekick.PortfolioViewer.Services.Implementation;
using GhostfolioSidekick.PortfolioViewer.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace GhostfolioSidekick.PortfolioViewer.Services;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPortfolioViewerServices(this IServiceCollection services)
    {
        // Register performance services
        services.AddScoped<IPortfolioValueService, PortfolioValueService>();
        services.AddScoped<IHoldingsPerformanceService, HoldingsPerformanceService>();
        services.AddScoped<IPerformanceAnalyticsService, PerformanceAnalyticsService>();
        services.AddScoped<IPortfolioOverviewService, PortfolioOverviewService>();

        return services;
    }
}