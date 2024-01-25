﻿using Newtonsoft.Json.Linq;

namespace GhostfolioSidekick.Cryptocurrency
{
	public class CryptoMapper
	{
		private Dictionary<string, string> mappings = new();

		private CryptoMapper()
		{
			var assembly = typeof(CryptoMapper).Assembly;
			using var resource = assembly.GetManifestResourceStream("GhostfolioSidekick.Cryptocurrency.cryptocurrencies.json");
			using var streamReader = new StreamReader(resource!);
			var fileContent = streamReader.ReadToEnd();
			var obj = JObject.Parse(fileContent);
			foreach (var item in obj)
			{
				mappings.Add(item.Key, item.Value!.ToString());
			}
		}

		public static readonly CryptoMapper Instance = new();

		public string GetFullname(string symbol)
		{
			if (string.IsNullOrWhiteSpace(symbol))
			{
				return symbol;
			}

			return mappings.TryGetValue(symbol, out var value) ? value : symbol;
		}
	}
}
