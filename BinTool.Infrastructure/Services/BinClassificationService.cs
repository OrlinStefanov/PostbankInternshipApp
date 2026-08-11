using BinTool.Application.Models.Classification;
using BinTool.Application.Abstractions;
using BinTool.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BinTool.Infrastructure.Services;

public class BinClassificationService : IBinClassificationService
{
    // Shortest stored prefix, so anything shorter cannot be classified at all.
    private const int MinPrefixLength = 6;

    // Longest stored prefix. Also the point at which the input is truncated: the tool never needs
    // more of a card number than this, so it never holds more.
    private const int MaxPrefixLength = 8;

    // Longest PAN under ISO/IEC 7812. Anything longer is not a card number.
    private const int MaxInputLength = 19;

    private readonly AppDbContext _db;
    private readonly ICommissionResolver _commissionResolver;
    private readonly ICardSchemeDetector _schemeDetector;

    public BinClassificationService(
        AppDbContext db,
        ICommissionResolver commissionResolver,
        ICardSchemeDetector schemeDetector)
    {
        _db = db;
        _commissionResolver = commissionResolver;
        _schemeDetector = schemeDetector;
    }

    public async Task<BinClassificationResult> ClassifyAsync(
        string bin, decimal? amount = null, string? amountCurrency = null,
        CancellationToken cancellationToken = default)
    {
        var lookupKey = Normalize(bin);

        // One query for all three candidate lengths, longest first: an 8-digit range is
        // more specific than the 6-digit range it sits inside, so it wins. The key ids come
        // back alongside the names so a fee can be resolved without a second lookup.
        var candidates = Candidates(lookupKey);

        var today = DateTime.UtcNow.Date;

        var match = await _db.BinRanges
            .AsNoTracking()
            .Where(b => !b.IsDeleted
                        && candidates.Contains(b.Prefix)
                        && b.ValidFrom <= today
                        && (b.ValidTo == null || b.ValidTo >= today))
            .OrderByDescending(b => b.PrefixLength)
            .Select(b => new MatchedRange
            {
                Prefix = b.Prefix,
                CardSchemeId = b.CardSchemeId,
                CardScheme = b.CardScheme!.Name,
                ProductTypeId = b.ProductTypeId,
                ProductType = b.ProductType!.Name,
                FundingTypeId = b.FundingTypeId,
                FundingType = b.FundingType!.Name,
                CountryCode = b.Country!.IsoCode,
                CountryName = b.Country.Name,
                RegionId = b.Country.RegionId,
                Region = b.Country.Region!.Name,
                ValidFrom = b.ValidFrom,
                ValidTo = b.ValidTo
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (match is null)
        {
            return new BinClassificationResult { Bin = lookupKey, Matched = false };
        }

        var result = new BinClassificationResult
        {
            Bin = lookupKey,
            Matched = true,
            MatchedPrefix = match.Prefix,
            CardScheme = match.CardScheme,
            ProductType = match.ProductType,
            FundingType = match.FundingType,
            CountryCode = match.CountryCode,
            CountryName = match.CountryName,
            Region = match.Region,
            ValidFrom = match.ValidFrom,
            ValidTo = match.ValidTo
        };

        // Flag when the stored range is under a scheme the detector's IIN rules disagree
        // with. Uses the matched prefix - what actually classified the card - rather than
        // the input, so a short 6-digit stored range is judged on its own digits.
        var detected = _schemeDetector.Detect(match.Prefix);
        var detectedName = _schemeDetector.DisplayName(detected);
        if (detectedName is not null && !_schemeDetector.Matches(detected, match.CardScheme))
        {
            result.DetectedScheme = detectedName;
        }

        // Pricing only when the caller asked for it. The card matched, so every key id is
        // present - the resolver decides whether any rule (or the default) applies.
        if (amount is { } transactionAmount)
        {
            var inputCurrencyId = await ResolveCurrencyIdAsync(amountCurrency, cancellationToken);

            result.Commission = await _commissionResolver.ResolveAsync(
                match.CardSchemeId, match.ProductTypeId, match.FundingTypeId, match.RegionId,
                transactionAmount, today, inputCurrencyId, cancellationToken);
        }

        return result;
    }

    // Maps a supplied ISO-4217 code to a live currency id, so the resolver can convert the amount
    // into the rule's currency. An unknown or blank code resolves to null, which the resolver reads
    // as the euro base currency.
    private async Task<int?> ResolveCurrencyIdAsync(
        string? code, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code)) return null;

        var upper = code.Trim().ToUpperInvariant();

        return await _db.Currencies.AsNoTracking()
            .Where(c => !c.IsDeleted && c.Code == upper)
            .Select(c => (int?)c.CurrencyId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private sealed class MatchedRange
    {
        public string Prefix { get; init; } = string.Empty;
        public int CardSchemeId { get; init; }
        public string CardScheme { get; init; } = string.Empty;
        public int ProductTypeId { get; init; }
        public string ProductType { get; init; } = string.Empty;
        public int FundingTypeId { get; init; }
        public string FundingType { get; init; } = string.Empty;
        public string CountryCode { get; init; } = string.Empty;
        public string CountryName { get; init; } = string.Empty;
        public int RegionId { get; init; }
        public string Region { get; init; } = string.Empty;
        public DateTime ValidFrom { get; init; }
        public DateTime? ValidTo { get; init; }
    }

    private static string Normalize(string bin)
    {
        if (string.IsNullOrWhiteSpace(bin))
        {
            throw new ArgumentException("A BIN is required.", nameof(bin));
        }

        var trimmed = bin.Trim();

        foreach (var c in trimmed)
        {
            if (!char.IsAsciiDigit(c))
            {
                // Deliberately does not echo the input - it may be a card number.
                throw new ArgumentException("The BIN must contain digits only.", nameof(bin));
            }
        }

        if (trimmed.Length < MinPrefixLength)
        {
            throw new ArgumentException(
                $"The BIN must be at least {MinPrefixLength} digits.", nameof(bin));
        }

        if (trimmed.Length > MaxInputLength)
        {
            throw new ArgumentException(
                $"The BIN must be at most {MaxInputLength} digits.", nameof(bin));
        }

        return trimmed.Length > MaxPrefixLength ? trimmed[..MaxPrefixLength] : trimmed;
    }

    private static string[] Candidates(string lookupKey)
    {
        var candidates = new string[lookupKey.Length - MinPrefixLength + 1];

        for (var length = MinPrefixLength; length <= lookupKey.Length; length++)
        {
            candidates[length - MinPrefixLength] = lookupKey[..length];
        }

        return candidates;
    }
}
