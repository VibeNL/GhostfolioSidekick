using GhostfolioSidekick.Model.Activities;

namespace GhostfolioSidekick.Model.Strategies
{
	public class RoundStrategy : IHoldingStrategy
	{
		public int Priority => (int)StrategiesPriority.Rounding;

		public Task Execute(Holding holding)
		{
			ArgumentNullException.ThrowIfNull(holding, nameof(holding));

			holding
				.Activities
				.OfType<IActivityWithQuantityAndUnitPrice>()
				.ToList()
				.ForEach(x =>
				{
					x.Quantity = Math.Round(x.Quantity, 10); // 10 is the maximum precision of the API

					if (x.UnitPrice != null)
					{
						x.UnitPrice = new Money(x.UnitPrice!.Currency, Math.Round(x.UnitPrice!.Amount, 10));
					}
				});

			return Task.CompletedTask;
		}
	}
}
