# Configuration Helper Documentation

The `ConfigurationHelper` provides a unified way to read configuration values from both `appsettings.json` files and environment variables, with environment variables taking precedence.

## Features

- **Automatic fallback**: Environment variables take precedence over appsettings.json values
- **Type conversion**: Automatic conversion to common types (string, int, bool, double, decimal, enums)
- **Configuration sections**: Bind entire configuration sections to strongly-typed models
- **Connection string support**: Special handling for database connection strings
- **Environment variable naming**: Automatic conversion of configuration keys to environment variable names

## Registration

The configuration helper is registered in `Program.cs`:

```csharp
builder.Services.AddSingleton<IConfigurationHelper, ConfigurationHelper>();
```

## Usage Examples

### Basic Configuration Values

```csharp
public class MyController : ControllerBase
{
    private readonly IConfigurationHelper _configHelper;

    public MyController(IConfigurationHelper configHelper)
    {
        _configHelper = configHelper;
    }

    public IActionResult GetSetting()
    {
        // Gets value from environment variable MYSETTING or appsettings.json "MySetting"
        var setting = _configHelper.GetConfigurationValue("MySetting", "default-value");
        
        // Gets value as specific type
        var timeout = _configHelper.GetConfigurationValue<int>("Timeout", 30);
        
        return Ok(new { setting, timeout });
    }
}
```

### Connection Strings

```csharp
// Gets connection string from environment variable CONNECTIONSTRING_DEFAULT
// or from appsettings.json ConnectionStrings:DefaultConnection
var connectionString = _configHelper.GetConnectionString("DefaultConnection");

// Custom connection string name
var reportingDb = _configHelper.GetConnectionString("ReportingConnection");
// Looks for CONNECTIONSTRING_REPORTING or ConnectionStrings:ReportingConnection
```

### Configuration Sections

```csharp
// Define a configuration model
public class GoogleSearchConfiguration
{
    public string ApiKey { get; set; } = string.Empty;
    public string EngineId { get; set; } = string.Empty;
}

// Use in controller
var googleConfig = _configHelper.GetConfigurationSection<GoogleSearchConfiguration>("GoogleSearch");
```

### Environment Variable Naming Convention

The configuration helper automatically converts configuration keys to environment variable names:

| Configuration Key | Environment Variable |
|-------------------|---------------------|
| `MySetting` | `MYSETTING` |
| `GoogleSearch:ApiKey` | `GOOGLESEARCH_APIKEY` |
| `Database:ConnectionString` | `DATABASE_CONNECTIONSTRING` |
| `App.Name` | `APP_NAME` |

### Connection String Environment Variables

| Connection String Name | Environment Variable |
|-----------------------|---------------------|
| `DefaultConnection` | `CONNECTIONSTRING_DEFAULT` |
| `ReportingConnection` | `CONNECTIONSTRING_REPORTING` |

## Configuration Priority

1. **Environment Variables** (highest priority)
2. **appsettings.{Environment}.json**
3. **appsettings.json**
4. **Default values** (lowest priority)

## Example appsettings.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=local.db"
  },
  "GoogleSearch": {
    "ApiKey": "your-api-key",
    "EngineId": "your-engine-id"
  },
  "Timeout": 30,
  "MySetting": "development-value"
}
```

## Example Environment Variables

```bash
# Override connection string
CONNECTIONSTRING_DEFAULT="Data Source=production.db"

# Override Google Search settings
GOOGLESEARCH_APIKEY="prod-api-key"
GOOGLESEARCH_ENGINEID="prod-engine-id"

# Override other settings
TIMEOUT=60
MYSETTING="production-value"
```

## Error Handling

The configuration helper throws `InvalidOperationException` when:
- A required configuration value is missing
- Type conversion fails
- An unsupported type is requested

Use try-catch blocks or provide default values to handle these cases gracefully.

## Supported Types

- `string`
- `int`
- `bool`
- `double`
- `decimal`
- `enum` types
- `Nullable<T>` versions of the above

## Best Practices

1. **Use strongly-typed configuration models** for complex settings
2. **Provide sensible default values** where possible
3. **Document environment variable names** for deployment teams
4. **Use the `HasConfigurationValue` method** to check if optional settings exist
5. **Keep environment variable names consistent** with the naming convention