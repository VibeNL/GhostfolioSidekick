using GhostfolioSidekick.Model;
using GhostfolioSidekick.Model.Market;

namespace GhostfolioSidekick.Model.Performance
{
public class UpcomingDividendsSnapshot
{
	public Guid Id { get; set; }
	public int HoldingId { get; set; }
	public DateOnly CalculationDate { get; set; }
	public decimal TotalExpectedReturn { get; set; }
	public Currency Currency { get; set; } = default!;
 public decimal TotalExpectedReturnPrimary { get; set; }
}
}
