using System;

namespace GhostfolioSidekick.Model.Market
{
    public record Dividend
    {
        public int Id { get; init; }
        public string Symbol { get; init; } = string.Empty;
        public DateOnly ExDividendDate { get; init; }
        public DateOnly PaymentDate { get; init; }
        public DividendType DividendType { get; init; }
        public DividendState DividendState { get; init; }
        public Money Amount { get; init; } = default!;
    }
}
