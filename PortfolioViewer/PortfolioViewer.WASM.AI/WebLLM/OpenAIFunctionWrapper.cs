using System.Text.Json;

namespace GhostfolioSidekick.PortfolioViewer.WASM.AI.WebLLM
{
	/// <summary>
	////var specs = new List<OpenAIFunctionWrapper>();
	////	foreach (var function in options?.Tools?.OfType<AIFunction>() ?? [])
	////	{
	////		specs.Add(new OpenAIFunctionWrapper()
	////{
	////	function = new OpenAIFunctionWrapper.Function()
	////	{
	////		name = function.Name,
	////		description = function.Description,
	////		parameters = function.JsonSchema
	////	}
	////		});
	////	}
	////	var toolsJson = JsonSerializer.Serialize(specs);
	/// </summary>
	public class OpenAIFunctionWrapper
	{
		public string type { get; set; } = "function";
		public Function function { get; set; }

		public class Function
		{
			public string name { get; set; }
			public string description { get; set; }
			public JsonElement parameters { get; set; }
		}
	}
}
