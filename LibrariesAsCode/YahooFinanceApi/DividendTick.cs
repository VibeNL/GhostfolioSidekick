using System;

namespace YahooFinanceApi
{
    public sealed class DividendTick : ITick
    {
        public DividendTick()
        {
        }

        internal DividendTick(DateTime dateTime, decimal dividend)
        {
            DateTime = dateTime;
            Dividend = dividend;
        }

        public DateTime DateTime { get; internal set; }

        public decimal Dividend { get; internal set; }
    }
}
