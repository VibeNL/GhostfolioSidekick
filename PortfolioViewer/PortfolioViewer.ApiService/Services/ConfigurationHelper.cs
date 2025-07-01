using Microsoft.Extensions.Configuration;

namespace GhostfolioSidekick.PortfolioViewer.ApiService.Services
{
    /// <summary>
    /// Configuration helper that provides a unified way to read configuration values
    /// from appsettings.json files and environment variables with proper fallback logic.
    /// </summary>
    public class ConfigurationHelper : IConfigurationHelper
    {
        private readonly IConfiguration _configuration;

        public ConfigurationHelper(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        /// <summary>
        /// Gets the database connection string from configuration or environment variable.
        /// Environment variable CONNECTIONSTRING_DEFAULT takes precedence over appsettings.json.
        /// </summary>
        public string GetConnectionString(string name = "DefaultConnection")
        {
            // First check environment variable with naming convention
            var envVarName = $"CONNECTIONSTRING_{name.ToUpper().Replace("CONNECTION", "")}";
            var envValue = Environment.GetEnvironmentVariable(envVarName);
            if (!string.IsNullOrEmpty(envValue))
            {
                return envValue;
            }

            // Fallback to configuration
            var configValue = _configuration.GetConnectionString(name);
            if (!string.IsNullOrEmpty(configValue))
            {
                return configValue;
            }

            throw new InvalidOperationException($"Connection string '{name}' not found in configuration or environment variable '{envVarName}'");
        }

        /// <summary>
        /// Gets a configuration value with fallback to environment variable.
        /// Environment variable takes precedence over appsettings.json.
        /// </summary>
        public string GetConfigurationValue(string key, string? defaultValue = null)
        {
            // First check environment variable (convert dots to underscores and uppercase)
            var envVarName = key.Replace(":", "_").Replace(".", "_").ToUpper();
            var envValue = Environment.GetEnvironmentVariable(envVarName);
            if (!string.IsNullOrEmpty(envValue))
            {
                return envValue;
            }

            // Fallback to configuration
            var configValue = _configuration[key];
            if (!string.IsNullOrEmpty(configValue))
            {
                return configValue;
            }

            if (defaultValue != null)
            {
                return defaultValue;
            }

            throw new InvalidOperationException($"Configuration value '{key}' not found in configuration or environment variable '{envVarName}'");
        }

        /// <summary>
        /// Gets a configuration value as a specific type with fallback to environment variable.
        /// </summary>
        public T GetConfigurationValue<T>(string key, T? defaultValue = default)
        {
            try
            {
                // First check environment variable
                var envVarName = key.Replace(":", "_").Replace(".", "_").ToUpper();
                var envValue = Environment.GetEnvironmentVariable(envVarName);
                if (!string.IsNullOrEmpty(envValue))
                {
                    return ConvertValue<T>(envValue);
                }

                // Fallback to configuration
                var configValue = _configuration[key];
                if (!string.IsNullOrEmpty(configValue))
                {
                    return ConvertValue<T>(configValue);
                }

                if (defaultValue != null)
                {
                    return defaultValue;
                }

                throw new InvalidOperationException($"Configuration value '{key}' not found in configuration or environment variable '{envVarName}'");
            }
            catch (Exception ex) when (!(ex is InvalidOperationException))
            {
                throw new InvalidOperationException($"Failed to convert configuration value '{key}' to type {typeof(T).Name}", ex);
            }
        }

        /// <summary>
        /// Gets a configuration section and binds it to a model with fallback to environment variables.
        /// </summary>
        public T GetConfigurationSection<T>(string sectionName) where T : new()
        {
            var section = _configuration.GetSection(sectionName);
            var model = new T();
            section.Bind(model);

            // Override with environment variables if they exist
            OverrideWithEnvironmentVariables(model, sectionName);

            return model;
        }

        /// <summary>
        /// Checks if a configuration key exists in either appsettings or environment variables.
        /// </summary>
        public bool HasConfigurationValue(string key)
        {
            var envVarName = key.Replace(":", "_").Replace(".", "_").ToUpper();
            return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(envVarName)) ||
                   !string.IsNullOrEmpty(_configuration[key]);
        }

        private static T ConvertValue<T>(string value)
        {
            var targetType = typeof(T);
            
            // Handle nullable types
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                targetType = Nullable.GetUnderlyingType(targetType)!;
            }

            if (targetType == typeof(string))
            {
                return (T)(object)value;
            }
            else if (targetType == typeof(int))
            {
                return (T)(object)int.Parse(value);
            }
            else if (targetType == typeof(bool))
            {
                return (T)(object)bool.Parse(value);
            }
            else if (targetType == typeof(double))
            {
                return (T)(object)double.Parse(value);
            }
            else if (targetType == typeof(decimal))
            {
                return (T)(object)decimal.Parse(value);
            }
            else if (targetType.IsEnum)
            {
                return (T)Enum.Parse(targetType, value, true);
            }

            throw new NotSupportedException($"Type {typeof(T).Name} is not supported for configuration conversion");
        }

        private static void OverrideWithEnvironmentVariables<T>(T model, string sectionName)
        {
            var properties = typeof(T).GetProperties();
            foreach (var property in properties)
            {
                if (property.CanWrite)
                {
                    var envVarName = $"{sectionName.ToUpper()}_{property.Name.ToUpper()}";
                    var envValue = Environment.GetEnvironmentVariable(envVarName);
                    if (!string.IsNullOrEmpty(envValue))
                    {
                        try
                        {
                            var convertedValue = ConvertValue(envValue, property.PropertyType);
                            property.SetValue(model, convertedValue);
                        }
                        catch
                        {
                            // Ignore conversion errors for environment variable overrides
                        }
                    }
                }
            }
        }

        private static object ConvertValue(string value, Type targetType)
        {
            // Handle nullable types
            if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
            {
                targetType = Nullable.GetUnderlyingType(targetType)!;
            }

            if (targetType == typeof(string))
            {
                return value;
            }
            else if (targetType == typeof(int))
            {
                return int.Parse(value);
            }
            else if (targetType == typeof(bool))
            {
                return bool.Parse(value);
            }
            else if (targetType == typeof(double))
            {
                return double.Parse(value);
            }
            else if (targetType == typeof(decimal))
            {
                return decimal.Parse(value);
            }
            else if (targetType.IsEnum)
            {
                return Enum.Parse(targetType, value, true);
            }

            throw new NotSupportedException($"Type {targetType.Name} is not supported for configuration conversion");
        }
    }
}