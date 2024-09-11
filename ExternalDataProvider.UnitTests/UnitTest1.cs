using GhostfolioSidekick.ExternalDataProvider;
using GhostfolioSidekick.Model;
using Microsoft.Extensions.Logging;
using Moq;

namespace ExternalDataProvider.UnitTests
{
	public class UnitTest1
	{
		[Fact]
		public async Task Test1()
		{
			var x = new CurrencyRepository(new Mock<ILogger<CurrencyRepository>>().Object);

			var r = await x.GetCurrencyHistory(Currency.EUR, Currency.USD, new DateOnly(1900, 1, 1));

		}
	}
}