namespace GhostfolioSidekick.Model.Performance
{
using GhostfolioSidekick.Model.Market;

public class UpcomingDividendTimelineEntry
{
	public Guid Id { get; set; }
	public int HoldingId { get; set; }
	public DateOnly ExpectedDate { get; set; }
	public decimal Amount { get; set; }
	public Currency Currency { get; set; } = default!;
	public decimal AmountPrimaryCurrency { get; set; }
	public DividendType DividendType { get; set; }
	public DividendState DividendState { get; set; }
}
}
