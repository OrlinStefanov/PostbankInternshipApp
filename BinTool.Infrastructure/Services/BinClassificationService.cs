using BinTool.Core.Models.Classification;
using BinTool.Core.Services;
using BinTool.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace BinTool.Infrastructure.Services;

public class BinClassificationService : IBinClassificationService
{
    /// <summary>
    /// Shortest stored prefix, so anything shorter cannot be classified at all.
    /// </summary>
    private const int MinPrefixLength = 6;

    /// <summary>
    /// Longest stored prefix. Also the point at which the input is truncated: the tool
    /// never needs more of a card number than this, so it never holds more.
    /// </summary>
    private const int MaxPrefixLength = 8;

    /// <summary>
    /// Longest PAN under ISO/IEC 7812. Anything longer is not a card number.
    /// </summary>
    private const int MaxInputLength = 19;

    private readonly AppDbContext _db;

    public BinClassificationService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<BinClassificationResult> ClassifyAsync(
        string bin, CancellationToken cancellationToken = default)
    {
        var lookupKey = Normalize(bin);

        // One query for all three candidate lengths, longest first: an 8-digit range is
        // more specific than the 6-digit range it sits inside, so it wins.
        var candidates = Candidates(lookupKey);
        var today = DateTime.UtcNow.Date;

        var match = await _db.BinRanges
            .AsNoTracking()
            .Where(b => !b.IsDeleted
                        && candidates.Contains(b.Prefix)
                        && b.ValidFrom <= today
                        && (b.ValidTo == null || b.ValidTo >= today))
            .OrderByDescending(b => b.PrefixLength)
            .Select(b => new BinClassificationResult
            {
                Bin = lookupKey,
                Matched = true,
                MatchedPrefix = b.Prefix,
                CardScheme = b.CardScheme!.Name,
                ProductType = b.ProductType!.Name,
                FundingType = b.FundingType!.Name,
                CountryCode = b.Country!.IsoCode,
                CountryName = b.Country.Name,
                Region = b.Country.Region!.Name,
                ValidFrom = b.ValidFrom,
                ValidTo = b.ValidTo
            })
            .FirstOrDefaultAsync(cancellationToken);

        return match ?? new BinClassificationResult { Bin = lookupKey, Matched = false };
    }

    /// <summary>
    /// Validates the input and reduces it to the lookup key. Truncation happens here,
    /// before the value reaches the query, so no code downstream can see a full PAN.
    /// </summary>
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

    /// <summary>
    /// Every stored prefix length the key could match, from 6 digits up to its own length.
    /// </summary>
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