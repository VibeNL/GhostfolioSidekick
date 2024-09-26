using GhostfolioSidekick.Configuration;
using GhostfolioSidekick.ExternalDataProvider.PolygonIO.Contract;
using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Market;
using GhostfolioSidekick.Model.Symbols;
using Microsoft.Extensions.Logging;
using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;

namespace GhostfolioSidekick.ExternalDataProvider.PolygonIO
{
	public class SymbolMatcher : ISymbolMatcher
	{
		private readonly Policy policy;
		private readonly ILogger<SymbolMatcher> logger;
		private readonly string apiKey;

		static HttpClient client = new HttpClient();

		public SymbolMatcher(ILogger<SymbolMatcher> logger, IApplicationSettings applicationSettings)
		{
			var retryPolicy = Policy
				.Handle<Exception>()
				.WaitAndRetry(5, x => TimeSpan.FromSeconds(60), (exception, timeSpan, retryCount, context) =>
				{
					logger.LogWarning("The request failed");
				});

			policy = retryPolicy;
			this.logger = logger;
			this.apiKey = applicationSettings.ConfigurationInstance?.Settings?.DataProviderPolygonIOApiKey;
		}

		public async Task<SymbolProfile?> MatchSymbol(PartialSymbolIdentifier[] identifiers)
		{
			try
			{
				if (string.IsNullOrWhiteSpace(apiKey))
				{
					return null;
				}

				foreach (var identifier in identifiers)
				{
					string requestUri = $"https://api.polygon.io/v3/reference/tickers?search={identifier.Identifier}&apiKey={apiKey}";
					HttpResponseMessage response = await client.GetAsync(requestUri);
					response.EnsureSuccessStatusCode();

					var r = await response.Content.ReadFromJsonAsync<SymbolQueryResult>() ?? throw new NotSupportedException();

					return null;
				}
			}
			catch (Exception e)
			{
				logger.LogError(e, "Failed to get currency history");
			}

			return null;
		}
	}
}
