using System.Text.Json;
using System.Text.Json.Serialization;

namespace GhostfolioSidekick.PortfolioViewer.WASM.Data.Services
{
	/// <summary>
	/// Shared base for API-backed data service implementations.
	/// </summary>
	public abstract class ApiServiceBase
	{
		protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
		{
			Converters = { new DateOnlyJsonConverter() }
		};

		private sealed class DateOnlyJsonConverter : JsonConverter<DateOnly>
		{
			public override DateOnly Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
				=> DateOnly.Parse(reader.GetString()!);

			public override void Write(Utf8JsonWriter writer, DateOnly value, JsonSerializerOptions options)
				=> writer.WriteStringValue(value.ToString("yyyy-MM-dd"));
		}
	}
}
