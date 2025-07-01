namespace GhostfolioSidekick.PortfolioViewer.ApiService.Models
{
    /// <summary>
    /// Configuration model for Google Search settings.
    /// Can be populated from appsettings.json GoogleSearch section or environment variables:
    /// - GOOGLESEARCH_APIKEY
    /// - GOOGLESEARCH_ENGINEID
    /// </summary>
    public class GoogleSearchConfiguration
    {
        public string ApiKey { get; set; } = string.Empty;
        public string EngineId { get; set; } = string.Empty;
    }

    /// <summary>
    /// Configuration model for application-wide settings.
    /// Environment variables take precedence over appsettings.json values.
    /// </summary>
    public class AppConfiguration
    {
        public string AllowedHosts { get; set; } = "*";
        public GoogleSearchConfiguration GoogleSearch { get; set; } = new();
    }
}