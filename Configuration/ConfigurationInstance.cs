
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GhostfolioSidekick.Configuration
{
	public class ConfigurationInstance
	{
		private static readonly JsonSerializerOptions options = new()
		{
			Converters =
				{
					new JsonStringEnumConverter()
				}
		};

		[JsonPropertyName("platforms")]
		public PlatformConfiguration[]? Platforms { get; set; }

		[JsonPropertyName("accounts")]
		public AccountConfiguration[]? Accounts { get; set; }

		[JsonPropertyName("mappings")]
		public Mapping[]? Mappings { get; set; }

		[JsonPropertyName("symbols")]
		public SymbolConfiguration[]? Symbols { get; set; }

		[JsonPropertyName("settings")]
		public Settings Settings { get; set; } = new Settings { DataProviderPreference = "YAHOO;COINGECKO", PrimaryCurrency = "EUR" };

		[JsonPropertyName("benchmarks")]
		public SymbolConfiguration[]? Benchmarks { get; set; }

		[JsonPropertyName("broker-api-connections")]
		public ApiBrokerConfiguration[]? BrokerApiConnections { get; set; }

		public static ConfigurationInstance? Parse(string configuration)
		{
			return JsonSerializer.Deserialize<ConfigurationInstance>(configuration, options);
		}

		public SymbolConfiguration? FindSymbol(string symbol)
		{
			return Symbols?.SingleOrDefault(x => x.Symbol == symbol);
		}
	}
}
