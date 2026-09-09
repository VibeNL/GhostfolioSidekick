using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace GhostfolioSidekick.Tools.ScraperUtilities.CliApi
{
	/// <summary>
	/// Trusted-device 2FA on login, parity with scalable-cli's handle_post_login_mfa.
	/// Runs after a successful device-code login: if the account has 2FA-on-login enabled and no approved session exists yet,
	/// start a challenge and poll until it is completed in the browser.
	/// </summary>
	public static class TrustedDevice2Fa
	{
		private const int PollIntervalSeconds = 2;
		private const int TimeoutSeconds = 120;

		private static readonly string Is2faOnLoginEnabledQuery = @"
query Is2faOnLoginEnabled($input: Is2faOnLoginEnabledInput!) {
  is2faOnLoginEnabled(input: $input) {
    enabled
    hasApprovedSession
  }
}";

		private static readonly string Start2faOnLoginMutation = @"
mutation Start2faOnLogin($input: Start2faOnLoginInput!) {
  start2faOnLogin(input: $input) {
    mfaSessionId
  }
}";

		private static readonly string Validate2faOnLoginMutation = @"
mutation Validate2faOnLogin($input: Validate2faOnLoginInput!) {
  validate2faOnLogin(input: $input) {
    status
  }
}";

		public static async Task EnsureApprovedSessionAsync(CliGraphqlClient client, string personId, ILogger logger, CancellationToken cancellationToken)
		{
			var state = await client.QueryAsync(Is2faOnLoginEnabledQuery, new { input = new { userId = personId } }, "Is2faOnLoginEnabled", cancellationToken);
			var enabledNode = state?["is2faOnLoginEnabled"]?["enabled"];
			if (enabledNode?.GetValueKind() != JsonValueKind.True)
			{
				return;
			}

			var approvedNode = state?["is2faOnLoginEnabled"]?["hasApprovedSession"];
			if (approvedNode?.GetValueKind() == JsonValueKind.True)
			{
				return;
			}

			var start = await client.QueryAsync(Start2faOnLoginMutation, new { input = new { userId = personId, deviceName = "CLI", deviceType = "CLI" } }, "Start2faOnLogin", cancellationToken);
			var mfaSessionId = start?["start2faOnLogin"]?["mfaSessionId"]?.GetValue<string>();
			if (string.IsNullOrWhiteSpace(mfaSessionId))
			{
				throw new CliApiException("2FA on login is enabled but mfaSessionId was not returned");
			}

			logger.LogInformation("2FA on login enabled; complete the challenge in your browser.");
			var deadline = DateTime.UtcNow.AddSeconds(TimeoutSeconds);
			while (true)
			{
				if (DateTime.UtcNow >= deadline)
				{
					throw new CliApiException("2FA login challenge timed out");
				}

				cancellationToken.ThrowIfCancellationRequested();
				var validate = await client.QueryAsync(Validate2faOnLoginMutation, new { input = new { userId = personId, mfaSessionId } }, "Validate2faOnLogin", cancellationToken);
				var status = validate?["validate2faOnLogin"]?["status"]?.GetValue<string>();
				switch (status)
				{
					case "SUCCESS":
						logger.LogInformation("2FA on login approved.");
						return;
					case "PENDING":
						await Task.Delay(TimeSpan.FromSeconds(PollIntervalSeconds), cancellationToken);
						continue;
					case "DENY":
						throw new CliApiException("2FA login challenge denied");
					case "TIMEOUT_RETRY":
						throw new CliApiException("2FA login timed out; please retry");
					case null:
						throw new CliApiException("Missing 2FA status in response");
					default:
						throw new CliApiException($"Unexpected 2FA status: {status}");
				}
			}
		}
	}
}
