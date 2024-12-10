using System;

namespace YahooFinanceApi
{
    public sealed class SplitTick : ITick
    {
        internal SplitTick(DateTime dateTime, decimal beforeSplit, decimal afterSplit)
        {
            DateTime = dateTime;
            BeforeSplit = beforeSplit;
            AfterSplit = afterSplit;
        }

        public DateTime DateTime { get; internal set;  }

        public decimal BeforeSplit { get; internal set; }

        public decimal AfterSplit { get; internal set; }
    }
}
