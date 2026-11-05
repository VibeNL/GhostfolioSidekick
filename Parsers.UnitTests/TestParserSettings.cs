using GhostfolioSidekick.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace GhostfolioSidekick.Parsers.UnitTests
{
	internal static class TestParserSettings
	{
		public static IApplicationSettings Create(bool skipUnknownTransactionTypes = false)
		{
			var settingsMock = new Mock<IApplicationSettings>();
			_ = settingsMock.Setup(x => x.ConfigurationInstance).Returns(new ConfigurationInstance
			{
				Settings = new Settings
				{
					DataProviderPreference = "YAHOO;COINGECKO",
					PrimaryCurrency = "EUR",
					SkipUnknownTransactionTypes = skipUnknownTransactionTypes,
				},
			});
			return settingsMock.Object;
		}

		public static ILogger<T> Logger<T>() => NullLogger<T>.Instance;
	}
}
