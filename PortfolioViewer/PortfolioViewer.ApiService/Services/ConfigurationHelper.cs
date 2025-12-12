using GhostfolioSidekick.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace GhostfolioSidekick.PortfolioViewer.ApiService.Services
{
	/// <summary>
	/// Configuration helper that provides a unified way to read configuration values
	/// from appsettings.json files and environment variables with proper fallback logic.
	/// </summary>
	[System.Diagnostics.CodeAnalysis.SuppressMessage("Performance", "CA1873:Avoid potentially expensive logging", Justification = "<Pending>")]
	public class ConfigurationHelper(IConfiguration configuration, IApplicationSettings applicationSettings, ILogger<ConfigurationHelper>? logger = null) : IConfigurationHelper
	{
		private readonly IConfiguration _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
		private readonly IApplicationSettings applicationSettings = applicationSettings ?? throw new ArgumentNullException(nameof(applicationSettings));
		private readonly ConcurrentDictionary<string, string> _envVarCache = new();
		private readonly ConcurrentDictionary<Type, TypeConverter> _typeConverters = new();

		public string GetConnectionString()
		{
			var path = applicationSettings.DatabaseFilePath;
			return path;
		}

		public string GetConfigurationValue(string key, string? defaultValue = null)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(key);

			var envVarName = NormalizeEnvironmentVariableName(key);
			var envValue = GetCachedEnvironmentVariable(envVarName);
			if (!string.IsNullOrEmpty(envValue))
			{
				logger?.LogDebug("Using configuration value '{Key}' from environment variable '{EnvVar}'", key, envVarName);
				return envValue;
			}

			var configValue = _configuration[key];
			if (!string.IsNullOrEmpty(configValue))
			{
				logger?.LogDebug("Using configuration value '{Key}' from configuration", key);
				return configValue;
			}

			if (defaultValue != null)
			{
				logger?.LogDebug("Using default value for configuration key '{Key}'", key);
				return defaultValue;
			}

			var message = $"Configuration value '{key}' not found in configuration or environment variable '{envVarName}'";
			logger?.LogError("{Message}", message);
			throw new InvalidOperationException(message);
		}

		public T GetConfigurationValue<T>(string key, T? defaultValue = default)
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(key);

			try
			{
				var envVarName = NormalizeEnvironmentVariableName(key);
				var envValue = GetCachedEnvironmentVariable(envVarName);
				if (!string.IsNullOrEmpty(envValue))
				{
					logger?.LogDebug("Using configuration value '{Key}' from environment variable '{EnvVar}'", key, envVarName);
					return ConvertValue<T>(envValue);
				}

				var configValue = _configuration[key];
				if (!string.IsNullOrEmpty(configValue))
				{
					logger?.LogDebug("Using configuration value '{Key}' from configuration", key);
					return ConvertValue<T>(configValue);
				}

				// Determine whether T is nullable/reference and whether a non-default value was provided
				var isNullableType = !typeof(T).IsValueType || Nullable.GetUnderlyingType(typeof(T)) != null;
				var defaultProvided = !EqualityComparer<T>.Default.Equals(defaultValue!, default!);

				if (defaultProvided || isNullableType)
				{
					logger?.LogDebug("Using default value for configuration key '{Key}'", key);
					return defaultValue!;
				}

				var message = $"Configuration value '{key}' not found in configuration or environment variable '{envVarName}'";
				logger?.LogError("{Message}", message);
				throw new InvalidOperationException(message);
			}
			catch (Exception ex) when (ex is not InvalidOperationException)
			{
				var message = $"Failed to convert configuration value '{key}' to type {typeof(T).Name}";
				logger?.LogError(ex, "{Message}", message);
				throw new InvalidOperationException(message, ex);
			}
		}

		public T GetConfigurationSection<T>(string sectionName) where T : new()
		{
			ArgumentException.ThrowIfNullOrWhiteSpace(sectionName);

			var section = _configuration.GetSection(sectionName);
			var model = new T();

			try
			{
				section.Bind(model);
				logger?.LogDebug("Bound configuration section '{SectionName}' to type {TypeName}", sectionName, typeof(T).Name);
			}
			catch (Exception ex)
			{
				logger?.LogWarning(ex, "Failed to bind configuration section '{SectionName}' to type {TypeName}", sectionName, typeof(T).Name);
			}

			OverrideWithEnvironmentVariables(model, sectionName);
			return model;
		}

		public bool HasConfigurationValue(string key)
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				return false;
			}

			var envVarName = NormalizeEnvironmentVariableName(key);
			return !string.IsNullOrEmpty(GetCachedEnvironmentVariable(envVarName)) ||
				   !string.IsNullOrEmpty(_configuration[key]);
		}

		private string GetCachedEnvironmentVariable(string name) =>
			_envVarCache.GetOrAdd(name, Environment.GetEnvironmentVariable(name) ?? string.Empty);

		private static string NormalizeEnvironmentVariableName(string key) =>
			key.Replace(":", "_", StringComparison.Ordinal)
			   .Replace(".", "_", StringComparison.Ordinal)
			   .ToUpperInvariant();

		private T ConvertValue<T>(string value)
		{
			var targetType = typeof(T);

			if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
			{
				targetType = Nullable.GetUnderlyingType(targetType)!;
			}

			// Fast path for common types
			return targetType.Name switch
			{
				nameof(String) => (T)(object)value,
				nameof(Int32) => (T)(object)int.Parse(value, CultureInfo.InvariantCulture),
				nameof(Int64) => (T)(object)long.Parse(value, CultureInfo.InvariantCulture),
				nameof(Boolean) => (T)(object)bool.Parse(value),
				nameof(Double) => (T)(object)double.Parse(value, CultureInfo.InvariantCulture),
				nameof(Decimal) => (T)(object)decimal.Parse(value, CultureInfo.InvariantCulture),
				nameof(Single) => (T)(object)float.Parse(value, CultureInfo.InvariantCulture),
				nameof(DateTime) => (T)(object)DateTime.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal),// Adjust to UTC
				nameof(DateTimeOffset) => (T)(object)DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal), // Adjust to UTC
				nameof(TimeSpan) => (T)(object)TimeSpan.Parse(value, CultureInfo.InvariantCulture),
				nameof(Guid) => (T)(object)Guid.Parse(value),
				_ => ConvertComplexType<T>(value, targetType)
			};
		}

		private T ConvertComplexType<T>(string value, Type targetType)
		{
			if (targetType.IsEnum)
			{
				return (T)Enum.Parse(targetType, value, true);
			}

			var converter = _typeConverters.GetOrAdd(targetType, TypeDescriptor.GetConverter);
			if (converter?.CanConvertFrom(typeof(string)) == true)
			{
				var convertedValue = converter.ConvertFromInvariantString(value);
				if (convertedValue != null)
					return (T)convertedValue;
			}

			throw new NotSupportedException($"Type {typeof(T).Name} is not supported for configuration conversion");
		}

		private void OverrideWithEnvironmentVariables<T>(T model, string sectionName)
		{
			var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
			foreach (var property in properties.Where(p => p.CanWrite))
			{
				var envVarName = $"{sectionName.ToUpperInvariant()}_{property.Name.ToUpperInvariant()}";
				var envValue = GetCachedEnvironmentVariable(envVarName);
				if (!string.IsNullOrEmpty(envValue))
				{
					try
					{
						var convertedValue = ConvertValueForType(envValue, property.PropertyType);
						property.SetValue(model, convertedValue);
						logger?.LogDebug("Overrode property {PropertyName} with environment variable {EnvVar}", property.Name, envVarName);
					}
					catch (Exception ex)
					{
						logger?.LogWarning(ex, "Failed to convert environment variable {EnvVar} to property {PropertyName} of type {PropertyType}",
							envVarName, property.Name, property.PropertyType.Name);
					}
				}
			}
		}

		private object ConvertValueForType(string value, Type targetType)
		{
			if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
				targetType = Nullable.GetUnderlyingType(targetType)!;

			return targetType.Name switch
			{
				nameof(String) => value,
				nameof(Int32) => int.Parse(value, CultureInfo.InvariantCulture),
				nameof(Int64) => long.Parse(value, CultureInfo.InvariantCulture),
				nameof(Boolean) => bool.Parse(value),
				nameof(Double) => double.Parse(value, CultureInfo.InvariantCulture),
				nameof(Decimal) => decimal.Parse(value, CultureInfo.InvariantCulture),
				nameof(Single) => float.Parse(value, CultureInfo.InvariantCulture),
				nameof(DateTime) => DateTime.Parse(value, CultureInfo.InvariantCulture),
				nameof(DateTimeOffset) => DateTimeOffset.Parse(value, CultureInfo.InvariantCulture),
				nameof(TimeSpan) => TimeSpan.Parse(value, CultureInfo.InvariantCulture),
				nameof(Guid) => Guid.Parse(value),
				_ => ConvertComplexTypeForType(value, targetType)
			};
		}

		private object ConvertComplexTypeForType(string value, Type targetType)
		{
			if (targetType.IsEnum)
			{
				return Enum.Parse(targetType, value, true);
			}

			var converter = _typeConverters.GetOrAdd(targetType, TypeDescriptor.GetConverter);
			if (converter?.CanConvertFrom(typeof(string)) == true)
			{
				var convertedValue = converter.ConvertFromInvariantString(value);
				if (convertedValue != null)
					return convertedValue;
			}

			throw new NotSupportedException($"Type {targetType.Name} is not supported for configuration conversion");
		}
	}
}