namespace BinTool.Domain.Common;

public static class DomainConstants
{
    // Every rate is stored as the euro value of one unit, so the euro row is the fixed point
    // the arithmetic pivots through. Its code was written out in the resolver, the currency
    // repository and the seed independently; a rename that missed one would have converted
    // silently and wrongly.
    public const string BaseCurrencyCode = "EUR";
}
