using GhostfolioSidekick.Database;
using GhostfolioSidekick.Database.Repository;
using GhostfolioSidekick.PortfolioViewer.Services.Implementation;
using GhostfolioSidekick.PortfolioViewer.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace GhostfolioSidekick.PortfolioViewer.Services.UnitTests;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddPortfolioViewerServices_ShouldRegisterAllServices()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPortfolioViewerServices();

        // Assert - Verify all services are registered by checking service descriptors
        var currencyExchangeDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ICurrencyExchange));
        var portfolioValueDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IPortfolioValueService));
        var holdingsPerformanceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IHoldingsPerformanceService));
        var performanceAnalyticsDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IPerformanceAnalyticsService));
        var portfolioOverviewDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IPortfolioOverviewService));

        Assert.NotNull(currencyExchangeDescriptor);
        Assert.NotNull(portfolioValueDescriptor);
        Assert.NotNull(holdingsPerformanceDescriptor);
        Assert.NotNull(performanceAnalyticsDescriptor);
        Assert.NotNull(portfolioOverviewDescriptor);
    }

    [Fact]
    public void AddPortfolioViewerServices_ShouldRegisterServicesAsScoped()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPortfolioViewerServices();

        // Assert - Verify all services are registered with Scoped lifetime
        var currencyExchangeDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ICurrencyExchange));
        var portfolioValueDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IPortfolioValueService));
        var holdingsPerformanceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IHoldingsPerformanceService));
        var performanceAnalyticsDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IPerformanceAnalyticsService));
        var portfolioOverviewDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IPortfolioOverviewService));

        Assert.NotNull(currencyExchangeDescriptor);
        Assert.NotNull(portfolioValueDescriptor);
        Assert.NotNull(holdingsPerformanceDescriptor);
        Assert.NotNull(performanceAnalyticsDescriptor);
        Assert.NotNull(portfolioOverviewDescriptor);

        Assert.Equal(ServiceLifetime.Scoped, currencyExchangeDescriptor.Lifetime);
        Assert.Equal(ServiceLifetime.Scoped, portfolioValueDescriptor.Lifetime);
        Assert.Equal(ServiceLifetime.Scoped, holdingsPerformanceDescriptor.Lifetime);
        Assert.Equal(ServiceLifetime.Scoped, performanceAnalyticsDescriptor.Lifetime);
        Assert.Equal(ServiceLifetime.Scoped, portfolioOverviewDescriptor.Lifetime);
    }

    [Fact]
    public void AddPortfolioViewerServices_ShouldReturnServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        var result = services.AddPortfolioViewerServices();

        // Assert
        Assert.Same(services, result);
    }

    [Fact]
    public void AddPortfolioViewerServices_ShouldRegisterCorrectImplementationTypes()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPortfolioViewerServices();

        // Assert - Check service descriptor implementation types
        var currencyExchangeDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ICurrencyExchange));
        var portfolioValueDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IPortfolioValueService));
        var holdingsPerformanceDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IHoldingsPerformanceService));
        var performanceAnalyticsDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IPerformanceAnalyticsService));
        var portfolioOverviewDescriptor = services.FirstOrDefault(s => s.ServiceType == typeof(IPortfolioOverviewService));

        Assert.Equal(typeof(CurrencyExchange), currencyExchangeDescriptor?.ImplementationType);
        Assert.Equal(typeof(PortfolioValueService), portfolioValueDescriptor?.ImplementationType);
        Assert.Equal(typeof(HoldingsPerformanceService), holdingsPerformanceDescriptor?.ImplementationType);
        Assert.Equal(typeof(PerformanceAnalyticsService), performanceAnalyticsDescriptor?.ImplementationType);
        Assert.Equal(typeof(PortfolioOverviewService), portfolioOverviewDescriptor?.ImplementationType);
    }

    [Fact]
    public void AddPortfolioViewerServices_ShouldAllowMultipleCalls()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act - Call multiple times
        services.AddPortfolioViewerServices();
        services.AddPortfolioViewerServices();

        // Assert - Should not throw and should be able to count services
        var portfolioValueDescriptors = services.Where(s => s.ServiceType == typeof(IPortfolioValueService)).ToList();
        Assert.True(portfolioValueDescriptors.Count >= 1); // At least one registration
    }

    [Fact]
    public void AddPortfolioViewerServices_ServiceDescriptors_ShouldHaveCorrectProperties()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPortfolioViewerServices();

        // Assert - Verify service descriptor properties
        var serviceTypes = new[]
        {
            typeof(ICurrencyExchange),
            typeof(IPortfolioValueService),
            typeof(IHoldingsPerformanceService),
            typeof(IPerformanceAnalyticsService),
            typeof(IPortfolioOverviewService)
        };

        foreach (var serviceType in serviceTypes)
        {
            var descriptor = services.FirstOrDefault(s => s.ServiceType == serviceType);
            Assert.NotNull(descriptor);
            Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
            Assert.NotNull(descriptor.ImplementationType);
            Assert.True(serviceType.IsAssignableFrom(descriptor.ImplementationType));
        }
    }

    [Fact]
    public void AddPortfolioViewerServices_ShouldRegisterExpectedServiceCount()
    {
        // Arrange
        var services = new ServiceCollection();
        var initialCount = services.Count;

        // Act
        services.AddPortfolioViewerServices();

        // Assert - Should register exactly 5 services
        var addedServices = services.Count - initialCount;
        Assert.Equal(5, addedServices);
    }

    [Fact]
    public void AddPortfolioViewerServices_ServiceTypes_ShouldBeInterfaces()
    {
        // Arrange
        var services = new ServiceCollection();

        // Act
        services.AddPortfolioViewerServices();

        // Assert - All registered service types should be interfaces
        var portfolioServiceDescriptors = services.Where(s => 
            s.ServiceType == typeof(ICurrencyExchange) ||
            s.ServiceType == typeof(IPortfolioValueService) ||
            s.ServiceType == typeof(IHoldingsPerformanceService) ||
            s.ServiceType == typeof(IPerformanceAnalyticsService) ||
            s.ServiceType == typeof(IPortfolioOverviewService)).ToList();

        Assert.Equal(5, portfolioServiceDescriptors.Count);
        
        foreach (var descriptor in portfolioServiceDescriptors)
        {
            Assert.True(descriptor.ServiceType.IsInterface);
            Assert.StartsWith("I", descriptor.ServiceType.Name);
        }
    }
}