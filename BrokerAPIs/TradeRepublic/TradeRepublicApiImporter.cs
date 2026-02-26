using GhostfolioSidekick.BrokerAPIs.TradeRepublic.Models;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GhostfolioSidekick.BrokerAPIs.TradeRepublic
{
	/// <summary>
	/// Imports Trade Republic transactions by downloading PDF documents from the
	/// Trade Republic postbox API and saving them to the file importer folder.
	/// The existing PDF parsers (TradeRepublicParser) then process the downloaded files.
	///
	/// Configuration options (under "options" in broker-api-connections):
	///   "phone-number" - Trade Republic account phone number (e.g. "+49123456789")
	///   "pin"          - Trade Republic PIN (5 digits)
	///
	/// Two-factor authentication:
	///   On first run (or after session expiry), Trade Republic sends a number-match
	///   2FA challenge. The importer writes a challenge file to the output directory
	///   and waits up to 5 minutes for a response file to appear.
	///   1. Check the log for the numbers and tap the matching one on your TR app.
	///   2. Create file ".tr_2fa_response.txt" in the account folder containing the
	///      number you tapped. The importer will complete authentication and continue.
	///   Session tokens are stored in ".tr_state.json" and reused on subsequent runs.
	/// </summary>
	public class TradeRepublicApiImporter : IApiBrokerImporter
	{
		private static readonly JsonSerializerOptions DeserializeOptions = new() { PropertyNameCaseInsensitive = true };

		private readonly IHttpClientFactory _httpClientFactory;

		public TradeRepublicApiImporter(IHttpClientFactory httpClientFactory)
		{
			_httpClientFactory = httpClientFactory;
		}

		public string BrokerType => "trade-republic";

		public async Task SyncAsync(
			string accountName,
			string outputDirectory,
			Dictionary<string, string> options,
			ILogger logger,
			CancellationToken cancellationToken = default)
		{
			if (!TryGetRequiredOption(options, "phone-number", out var phoneNumber, logger) ||
				!TryGetRequiredOption(options, "pin", out var pin, logger))
			{
				return;
			}

			var stateFilePath = Path.Combine(outputDirectory, ".tr_state.json");
			var state = LoadState(stateFilePath);

			await using var client = new TradeRepublicApiClient();
			await client.ConnectAsync(cancellationToken);

			var sessionToken = await AuthenticateAsync(client, phoneNumber!, pin!, state, outputDirectory, logger, cancellationToken);
			if (sessionToken == null)
			{
				logger.LogWarning("Trade Republic: authentication incomplete, will retry on next run");
				return;
			}

			state.SessionToken = sessionToken;
			SaveState(stateFilePath, state);

			await SyncDocumentsAsync(client, state, outputDirectory, logger, cancellationToken);
			SaveState(stateFilePath, state);
		}

		private async Task<string?> AuthenticateAsync(
			TradeRepublicApiClient client,
			string phoneNumber,
			string pin,
			TradeRepublicState state,
			string outputDirectory,
			ILogger logger,
			CancellationToken cancellationToken)
		{
			var loginJson = state.SessionToken != null
				? JsonSerializer.Serialize(new { type = "login", phoneNumber, pin, sessionToken = state.SessionToken })
				: JsonSerializer.Serialize(new { type = "login", phoneNumber, pin });

			var response = await client.SendRequestAsync(loginJson, cancellationToken);
			var loginResponse = response.Deserialize<TradeRepublicLoginResponse>(DeserializeOptions);

			if (loginResponse?.SessionToken != null)
			{
				logger.LogInformation("Trade Republic: authenticated successfully");
				return loginResponse.SessionToken;
			}

			if (loginResponse?.NumbersToCombine is { Length: > 0 } numbers)
			{
				return await HandleTwoFactorAsync(client, numbers, outputDirectory, logger, cancellationToken);
			}

			logger.LogError("Trade Republic: unexpected authentication response");
			return null;
		}

		private static async Task<string?> HandleTwoFactorAsync(
			TradeRepublicApiClient client,
			int[] numbers,
			string outputDirectory,
			ILogger logger,
			CancellationToken cancellationToken)
		{
			var challengeFile = Path.Combine(outputDirectory, ".tr_2fa_challenge.txt");
			var responseFile = Path.Combine(outputDirectory, ".tr_2fa_response.txt");
			var numbersStr = string.Join(", ", numbers);

			await File.WriteAllTextAsync(
				challengeFile,
				$"Trade Republic 2FA required.\n" +
				$"Numbers: [{numbersStr}]\n" +
				$"Tap the matching number on your Trade Republic phone app,\n" +
				$"then create the file '{responseFile}' containing only the number you tapped.",
				cancellationToken);

			logger.LogWarning(
				"Trade Republic 2FA required. Numbers: [{Numbers}]. " +
				"Tap the matching number on your Trade Republic app, then create file '{ResponseFile}' with that number.",
				numbersStr, responseFile);

			var timeout = DateTime.UtcNow.AddMinutes(5);
			while (DateTime.UtcNow < timeout && !cancellationToken.IsCancellationRequested)
			{
				if (File.Exists(responseFile))
				{
					var answer = (await File.ReadAllTextAsync(responseFile, cancellationToken)).Trim();
					File.Delete(responseFile);
					File.Delete(challengeFile);

					var twoFaJson = JsonSerializer.Serialize(new
					{
						type = "secondFactorAuthentication",
						codeType = "numberMatch",
						answer
					});

					var response = await client.SendRequestAsync(twoFaJson, cancellationToken);
					var loginResponse = response.Deserialize<TradeRepublicLoginResponse>(DeserializeOptions);

					if (loginResponse?.SessionToken != null)
					{
						logger.LogInformation("Trade Republic: 2FA completed successfully");
						return loginResponse.SessionToken;
					}

					logger.LogError("Trade Republic: 2FA response did not contain a session token");
					return null;
				}

				await Task.Delay(TimeSpan.FromSeconds(5), cancellationToken);
			}

			logger.LogWarning(
				"Trade Republic: 2FA timed out. Challenge file remains at '{ChallengeFile}'. Will retry on next run.",
				challengeFile);
			return null;
		}

		private async Task SyncDocumentsAsync(
			TradeRepublicApiClient client,
			TradeRepublicState state,
			string outputDirectory,
			ILogger logger,
			CancellationToken cancellationToken)
		{
			var allItems = new List<PostboxItem>();
			string? cursor = null;

			do
			{
				var requestJson = cursor == null
					? JsonSerializer.Serialize(new { type = "postboxV2" })
					: JsonSerializer.Serialize(new { type = "postboxV2", after = cursor });

				var response = await client.SubscribeOnceAsync(requestJson, cancellationToken);
				var postboxResponse = response.Deserialize<PostboxResponse>(DeserializeOptions);

				if (postboxResponse?.Items != null)
					allItems.AddRange(postboxResponse.Items);

				cursor = postboxResponse?.Cursors?.After;
			}
			while (cursor != null);

			logger.LogInformation("Trade Republic: found {Count} total documents, checking for new ones", allItems.Count);

			var newCount = 0;
			foreach (var item in allItems)
			{
				if (item.Id == null) continue;
				if (state.DownloadedDocumentIds.Contains(item.Id)) continue;

				var docId = item.Detail?.Action?.Id ?? item.Id;
				var downloadUrl = await GetDocumentDownloadUrlAsync(client, docId, logger, cancellationToken);

				if (downloadUrl != null)
				{
					var date = item.Date ?? DateTimeOffset.UtcNow.ToString("yyyy-MM-dd");
					var title = SanitizeFileName(item.Title ?? item.Id[..Math.Min(item.Id.Length, 8)]);
					var suffix = item.Id.Length >= 8 ? item.Id[..8] : item.Id;
					var fileName = $"{date}_{title}_{suffix}.pdf";
					var filePath = Path.Combine(outputDirectory, fileName);

					if (await DownloadPdfAsync(downloadUrl, filePath, logger, cancellationToken))
						newCount++;
				}

				// Mark as processed regardless of download success to avoid infinite retries
				state.DownloadedDocumentIds.Add(item.Id);
			}

			logger.LogInformation("Trade Republic: downloaded {NewCount} new documents", newCount);
		}

		private static async Task<string?> GetDocumentDownloadUrlAsync(
			TradeRepublicApiClient client,
			string documentId,
			ILogger logger,
			CancellationToken cancellationToken)
		{
			try
			{
				var requestJson = JsonSerializer.Serialize(new { type = "postboxDocumentDetail", id = documentId });
				var response = await client.SubscribeOnceAsync(requestJson, cancellationToken);
				var detail = response.Deserialize<PostboxDocumentDetail>(DeserializeOptions);
				return detail?.Url;
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Trade Republic: failed to get download URL for document '{DocumentId}'", documentId);
				return null;
			}
		}

		private async Task<bool> DownloadPdfAsync(string url, string filePath, ILogger logger, CancellationToken cancellationToken)
		{
			try
			{
				var httpClient = _httpClientFactory.CreateClient();
				var bytes = await httpClient.GetByteArrayAsync(url, cancellationToken);
				await File.WriteAllBytesAsync(filePath, bytes, cancellationToken);
				return true;
			}
			catch (Exception ex)
			{
				logger.LogWarning(ex, "Trade Republic: failed to download PDF from '{Url}'", url);
				return false;
			}
		}

		private static string SanitizeFileName(string name)
		{
			var invalid = Path.GetInvalidFileNameChars();
			return new string(name.Where(c => !invalid.Contains(c)).ToArray()).Trim();
		}

		private static TradeRepublicState LoadState(string stateFilePath)
		{
			if (!File.Exists(stateFilePath)) return new TradeRepublicState();
			try
			{
				var json = File.ReadAllText(stateFilePath);
				return JsonSerializer.Deserialize<TradeRepublicState>(json) ?? new TradeRepublicState();
			}
			catch
			{
				return new TradeRepublicState();
			}
		}

		private static void SaveState(string stateFilePath, TradeRepublicState state)
		{
			var json = JsonSerializer.Serialize(state);
			File.WriteAllText(stateFilePath, json);
		}

		private static bool TryGetRequiredOption(Dictionary<string, string> options, string key, out string? value, ILogger logger)
		{
			if (options.TryGetValue(key, out value) && !string.IsNullOrEmpty(value))
				return true;
			logger.LogError("Trade Republic: required option '{Key}' is missing from broker API connection configuration", key);
			value = null;
			return false;
		}
	}
}
