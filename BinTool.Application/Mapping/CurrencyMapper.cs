using BinTool.Application.Models.Audit;
using BinTool.Application.Models.Currency;
using BinTool.Application.Models.ReferenceData;

namespace BinTool.Application.Mapping;

public static class CurrencyMapper
{
    // A code is stored upper-cased so uniqueness and lookup never depend on how it was typed.
    public static string NormalizedCode(this CurrencyInput input) =>
        input.Code.Trim().ToUpperInvariant();

    public static void Apply(Currency currency, CurrencyInput input)
    {
        currency.Code = input.NormalizedCode();
        currency.Name = input.Name.Trim();
        currency.RateToEur = input.RateToEur;
        currency.IsActive = input.IsActive;
    }

    public static CurrencySnapshot ToSnapshot(Currency c) =>
        new(c.Code, c.Name, c.RateToEur, c.IsActive, c.IsDeleted);

    public static CurrencyListItem ToListItem(Currency c) => new()
    {
        Id = c.CurrencyId,
        Code = c.Code,
        Name = c.Name,
        RateToEur = c.RateToEur,
        IsActive = c.IsActive,
        Status = c.IsDeleted ? LookupStatus.Deleted : LookupStatus.Active,
        DeletedAt = c.DeletedAt,
        DeletedBy = c.DeletedBy
    };
}
