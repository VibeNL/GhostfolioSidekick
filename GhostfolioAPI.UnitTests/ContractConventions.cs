using AutoFixture;
using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.GhostfolioAPI.UnitTests
{
	internal class ContractConventions : ICustomization
	{
		public void Customize(IFixture fixture)
		{
			fixture.Customize<Contract.Activity>(composer =>
			composer
				.With(p => p.Type, Contract.ActivityType.BUY));
			fixture.Customize<Contract.SymbolProfile>(composer =>
			composer
				.With(p => p.AssetClass, AssetClass.Equity.ToString().ToUpperInvariant())
				.With(p => p.AssetSubClass, AssetSubClass.Etf.ToString().ToUpperInvariant()));
		}
	}
}
