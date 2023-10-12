
using System.Text.Json;
using System.Text.Json.Serialization;

namespace GhostfolioSidekick.Configuration
{
	public class ConfigurationInstance
	{
		[JsonPropertyName("mappings")]
		public Mapping[] Mappings { get; set; }

		[JsonPropertyName("symbols")]
		public SymbolConfiguration[] Symbols { get; set; }

		public static ConfigurationInstance? Parse(string configuration)
		{
			JsonSerializerOptions options = new JsonSerializerOptions
			{
				Converters ={
					new JsonStringEnumConverter()
				}
			};

			return JsonSerializer.Deserialize<ConfigurationInstance>(configuration, options);
		}
	}
}
