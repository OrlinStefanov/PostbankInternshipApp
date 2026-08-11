namespace BinTool.Application.Models.ReferenceData;

/// <summary>
/// Which of the four Name+Description reference tables a call operates on. Countries
/// are shaped differently (ISO code + region assignment) and use their own service.
/// </summary>
public enum LookupKind
{
    CardScheme = 1,
    ProductType = 2,
    FundingType = 3,
    Region = 4
}
