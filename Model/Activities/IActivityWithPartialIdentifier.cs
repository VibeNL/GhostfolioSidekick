namespace GhostfolioSidekick.Model.Activities
{
	public interface IActivityWithPartialIdentifier
	{
		IList<PartialSymbolIdentifier> PartialSymbolIdentifiers { get; set; }
	}
}
