using GhostfolioSidekick.Model.Activities;
using GhostfolioSidekick.Model.Symbols;

namespace GhostfolioSidekick.Model.Performance
{
	public class HoldingAggregated
	{
		public string Symbol { get; set; } = string.Empty;

		public string? Name { get; set; }

		public string DataSource { get; set; } = string.Empty;

		public AssetClass AssetClass { get; set; }

		public AssetSubClass? AssetSubClass { get; set; }

		public int ActivityCount { get; set; }

		public virtual ICollection<CountryWeight> CountryWeight { get; set; } = [];

		public virtual ICollection<SectorWeight> SectorWeights { get; set; } = [];

		public virtual ICollection<CalculatedSnapshot> CalculatedSnapshots { get; set; } = [];

		public override string ToString()
		{
			return $"{Symbol} ({DataSource}) - {Name ?? "No Name"}";
		}
	}
}