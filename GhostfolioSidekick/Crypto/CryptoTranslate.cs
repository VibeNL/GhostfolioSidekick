using Newtonsoft.Json.Linq;
using System.Reflection;

namespace GhostfolioSidekick.Crypto
{
	public class CryptoTranslate
	{
		public static readonly CryptoTranslate Instance = new CryptoTranslate();
		private readonly JObject dictionary;

		public CryptoTranslate()
		{
			var assembly = Assembly.GetExecutingAssembly();
			var resourceName = "GhostfolioSidekick.Crypto.cryptocurrencies.json";

			using (Stream stream = assembly.GetManifestResourceStream(resourceName))
			{
				using (StreamReader reader = new StreamReader(stream))
				{
					string jsonFile = reader.ReadToEnd();
					dictionary = JObject.Parse(jsonFile);
				}
			}
		}

		public string TranslateToken(string source)
		{
			if (dictionary.ContainsKey(source))
			{
				return dictionary[source].ToString();
			}

			throw new NotSupportedException($"Crypto unknown {source}");
		}
	}
}
