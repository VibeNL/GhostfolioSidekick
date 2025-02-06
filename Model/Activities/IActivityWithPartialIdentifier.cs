namespace GhostfolioSidekick.Model.Activities
{
	public interface IActivityWithPartialIdentifier
	{
		List<PartialSymbolIdentifier> PartialSymbolIdentifiers { get; set; }
	}
}
