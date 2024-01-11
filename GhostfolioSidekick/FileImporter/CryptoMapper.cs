using Newtonsoft.Json.Linq;

namespace GhostfolioSidekick.FileImporter
{
	internal class CryptoMapper
	{
		private Dictionary<string, string> mappings = new();

		private CryptoMapper()
		{
			var assembly = typeof(CryptoMapper).Assembly;
			using var resource = assembly.GetManifestResourceStream("GhostfolioSidekick.FileImporter.cryptocurrencies.json");
			using var streamReader = new StreamReader(resource!);
			var fileContent = streamReader.ReadToEnd();
			var obj = JObject.Parse(fileContent);
			foreach (var item in obj)
			{
				mappings.Add(item.Key, item.Value!.ToString());
			}
		}

		public static readonly CryptoMapper Instance = new();

		internal string GetFullname(string symbol)
		{
			return mappings.TryGetValue(symbol, out var value) ? value : symbol;
		}
	}
}
