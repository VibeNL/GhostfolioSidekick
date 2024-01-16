namespace GhostfolioSidekick.Model
{
	public class Activity(
		ActivityType activityType,
		SymbolProfile? asset)
	{
		public ActivityType ActivityType { get; } = activityType;
		public SymbolProfile? Asset { get; } = asset;
	}
}