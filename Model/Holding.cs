namespace GhostfolioSidekick.Model
{
	public class Holding(SymbolProfile symbolProfile)
	{
		private readonly List<PartialActivity> partialActivities = new List<PartialActivity>();

		public SymbolProfile SymbolProfile { get; set; } = symbolProfile;

		public IEnumerable<Activity> Activities { get; set; } = new List<Activity>();

		public void AddPartialActivities(IEnumerable<PartialActivity> partials)
		{
			partialActivities.AddRange(partials);
		}

		public void DetermineActivities()
		{
			throw new NotSupportedException();
		}
	}
}
