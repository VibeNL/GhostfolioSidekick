using GhostfolioSidekick.Configuration;
using Moq;

namespace GhostfolioSidekick.Parsers.UnitTests
{
	public static class TestApplicationSettings
	{
		public static IApplicationSettings Create(bool warnOnUnsupportedRow = false)
		{
			var settings = new Mock<IApplicationSettings>();
			settings.Setup(x => x.ConfigurationInstance).Returns(new ConfigurationInstance
			{
				Settings = new Settings
				{
					DataProviderPreference = "YAHOO;COINGECKO",
					PrimaryCurrency = "EUR",
					WarnOnUnsupportedRow = warnOnUnsupportedRow
				}
			});
			return settings.Object;
		}
	}
}
