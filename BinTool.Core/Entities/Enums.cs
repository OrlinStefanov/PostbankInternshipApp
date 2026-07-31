namespace BinTool.Core.Entities;

/// <summary>
/// Payment card schemes (networks)
/// </summary>
public enum CardScheme
{
    Unknown = 0,
    Visa = 1,
    Mastercard = 2,
    AmericanExpress = 3,
    DinersClub = 4,
}

/// <summary>
/// Card product types
/// </summary>
public enum ProductType
{
    Unknown = 0,
    Consumer = 1,
    Commercial = 2,
    Prepaid = 3,
}

/// <summary>
/// Card funding types
/// </summary>
public enum FundingType
{
    Unknown = 0,
    Credit = 1,
    Debit = 2,
}

/// <summary>
/// Geographic region for fee determination
/// </summary>
public enum RegionType
{
    Unknown = 0,
    Domestic = 1,
    IntraEEA = 2,
    InterRegional = 3,
}
