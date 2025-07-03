namespace GhostfolioSidekick.PortfolioViewer.ApiService.Services
{
    /// <summary>
    /// Interface for configuration helper that provides unified access to configuration values
    /// from appsettings.json files and environment variables.
    /// </summary>
    public interface IConfigurationHelper
    {
        /// <summary>
        /// Gets the database connection string from configuration or environment variable.
        /// </summary>
        /// <param name="name">The name of the connection string (defaults to "DefaultConnection")</param>
        /// <returns>The connection string value</returns>
        string GetConnectionString(string name = "DefaultConnection");

        /// <summary>
        /// Gets a configuration value with fallback to environment variable.
        /// </summary>
        /// <param name="key">The configuration key</param>
        /// <param name="defaultValue">Default value if key is not found</param>
        /// <returns>The configuration value</returns>
        string GetConfigurationValue(string key, string? defaultValue = null);

        /// <summary>
        /// Gets a configuration value as a specific type with fallback to environment variable.
        /// </summary>
        /// <typeparam name="T">The type to convert the value to</typeparam>
        /// <param name="key">The configuration key</param>
        /// <param name="defaultValue">Default value if key is not found</param>
        /// <returns>The configuration value converted to the specified type</returns>
        T GetConfigurationValue<T>(string key, T? defaultValue = default);

        /// <summary>
        /// Gets a configuration section and binds it to a model with fallback to environment variables.
        /// </summary>
        /// <typeparam name="T">The type to bind the section to</typeparam>
        /// <param name="sectionName">The name of the configuration section</param>
        /// <returns>The bound configuration model</returns>
        T GetConfigurationSection<T>(string sectionName) where T : new();

        /// <summary>
        /// Checks if a configuration key exists in either appsettings or environment variables.
        /// </summary>
        /// <param name="key">The configuration key to check</param>
        /// <returns>True if the key exists, false otherwise</returns>
        bool HasConfigurationValue(string key);
    }
}