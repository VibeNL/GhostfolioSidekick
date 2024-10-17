using System;
using System.Collections.Generic;

namespace YahooFinanceApi.Tests;

public class DecimalComparerWithPrecision : IEqualityComparer<decimal>
{
    private readonly decimal precision;

    public DecimalComparerWithPrecision(decimal precision)
    {
        this.precision = precision;
    }

    public bool Equals(decimal x, decimal y)
    {
        return Math.Abs(x - y) < precision;
    }

    public int GetHashCode(decimal obj)
    {
        return obj.GetHashCode();
    }

    public static DecimalComparerWithPrecision defaultComparer = new DecimalComparerWithPrecision(0.00001m);
    public static DecimalComparerWithPrecision Default => defaultComparer;
}