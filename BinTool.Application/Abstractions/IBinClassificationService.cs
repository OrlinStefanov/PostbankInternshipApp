using BinTool.Application.Models.Classification;

namespace BinTool.Application.Abstractions;

public interface IBinClassificationService
{
    Task<BinClassificationResult> ClassifyAsync(
        string bin, decimal? amount = null, string? amountCurrency = null,
        CancellationToken cancellationToken = default);
}
