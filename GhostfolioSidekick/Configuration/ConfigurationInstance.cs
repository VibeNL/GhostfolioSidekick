
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GhostfolioSidekick.Configuration
{
	public class ConfigurationInstance
	{
		[JsonPropertyName("platforms")]
		public PlatformConfiguration[] Platforms { get; set; }

		[JsonPropertyName("accounts")]
		public AccountConfiguration[] Accounts { get; set; }

		[JsonPropertyName("mappings")]
		public Mapping[] Mappings { get; set; }

		[JsonPropertyName("symbols")]
		public SymbolConfiguration[] Symbols { get; set; }

		public static ConfigurationInstance? Parse(string configuration)
		{
			JsonSerializerOptions options = new()
			{
				Converters ={
					new JsonStringEnumConverter()
				}
			};

			return JsonSerializer.Deserialize<ConfigurationInstance>(configuration, options);
		}

		internal SymbolConfiguration? FindSymbol(string symbol)
		{
			return Symbols.SingleOrDefault(x => x.Symbol == symbol);
		}
	}
}
