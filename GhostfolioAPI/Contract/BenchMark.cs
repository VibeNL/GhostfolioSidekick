using System.Diagnostics.CodeAnalysis;

namespace GhostfolioSidekick.GhostfolioAPI.Contract
{
	[ExcludeFromCodeCoverage]
	public class BenchMark
	{
		public string? DataSource { get; set; }

		public Guid Id { get; set; }

		public required string Name { get; set; }

		public required string Symbol { get; set; }
	}
}