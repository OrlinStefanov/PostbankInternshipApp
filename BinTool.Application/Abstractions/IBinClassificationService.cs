using BinTool.Application.Models.Classification;

namespace BinTool.Application.Abstractions;

public interface IBinClassificationService
{
    /// <summary>
    /// Resolves the card attributes for a BIN by matching it against the stored BIN ranges, longest
    /// prefix first, considering only ranges that are valid today.
    /// </summary>
    Task<BinClassificationResult> ClassifyAsync(
        string bin, decimal? amount = null, string? amountCurrency = null,
        CancellationToken cancellationToken = default);
}
