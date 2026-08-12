using BinTool.Application.Abstractions;
using BinTool.Application.Mapping;
using BinTool.Application.Models.Classification;
using BinTool.Domain.Common;

namespace BinTool.Application.Services;

public class BinClassificationService : IBinClassificationService
{
    private readonly IBinRangeRepository _ranges;
    private readonly ICurrencyRepository _currencies;
    private readonly ICommissionResolver _commissionResolver;
    private readonly ICardSchemeDetector _schemeDetector;

    public BinClassificationService(
        IBinRangeRepository ranges,
        ICurrencyRepository currencies,
        ICommissionResolver commissionResolver,
        ICardSchemeDetector schemeDetector)
    {
        _ranges = ranges;
        _currencies = currencies;
        _commissionResolver = commissionResolver;
        _schemeDetector = schemeDetector;
    }

    public async Task<BinClassificationResult> ClassifyAsync(
        string bin, decimal? amount = null, string? amountCurrency = null,
        CancellationToken cancellationToken = default)
    {
        var lookupKey = BinPrefix.Normalize(bin);
        var today = DateTime.UtcNow.Date;

        var match = await _ranges.FindLongestMatchAsync(
            BinPrefix.Candidates(lookupKey), today, cancellationToken);

        if (match is null)
        {
            return BinClassificationResultMapper.NoMatch(lookupKey);
        }

        var result = BinClassificationResultMapper.From(
            lookupKey, match, _schemeDetector.MismatchedName(match.Prefix, match.CardScheme));

        if (amount is { } transactionAmount)
        {
            var inputCurrencyId = await _currencies.FindLiveIdByCodeAsync(
                amountCurrency, cancellationToken);

            result.Commission = await _commissionResolver.ResolveAsync(
                match.CardSchemeId, match.ProductTypeId, match.FundingTypeId, match.RegionId,
                transactionAmount, today, inputCurrencyId, cancellationToken);
        }

        return result;
    }
}
