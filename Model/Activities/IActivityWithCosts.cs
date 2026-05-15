namespace GhostfolioSidekick.Model.Activities
{
	public interface IActivityWithCosts
	{
		IReadOnlyList<Money> Costs { get; }
	}
}
