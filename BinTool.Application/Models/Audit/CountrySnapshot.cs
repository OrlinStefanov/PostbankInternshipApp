namespace BinTool.Application.Models.Audit;

public sealed record CountrySnapshot(
    string IsoCode, string Name, string Region, bool IsDeleted);
