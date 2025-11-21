using System;

namespace GhostfolioSidekick.Model.Market
{
    public record UpcomingDividend(
        int Id,
        string Symbol,
        DateOnly ExDividendDate,
        DateOnly PaymentDate,
        Money Amount
    );
}
