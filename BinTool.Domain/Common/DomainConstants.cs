namespace BinTool.Domain.Common;

public static class DomainConstants
{
    // Every rate is the euro value of one unit, so the euro row is the fixed point the arithmetic
    // pivots through. Its code was written out in three places independently; a rename that missed
    // one would have converted silently and wrongly.
    public const string BaseCurrencyCode = "EUR";
}
