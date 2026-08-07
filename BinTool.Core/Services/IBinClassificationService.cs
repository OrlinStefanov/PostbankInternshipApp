using BinTool.Core.Models.Classification;

namespace BinTool.Core.Services;

public interface IBinClassificationService
{
    /// <summary>
    /// Resolves the card attributes for a BIN by matching it against the stored BIN
    /// ranges, longest prefix first, considering only ranges that are valid today.
    /// </summary>
    /// <param name="bin">
    /// The BIN, or a full card number to take it from. Only the leading 8 digits are used.
    /// </param>
    /// <param name="amount">
    /// An optional transaction amount. When supplied and the BIN matches, the result also
    /// carries the resolved commission rule and the calculated fee.
    /// </param>
    /// <param name="amountCurrency">
    /// The ISO-4217 code <paramref name="amount"/> is given in. When the applied rule is
    /// priced in a different currency the amount is converted first. Defaults to euro when
    /// null or unrecognised.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="ArgumentException">
    /// The input is empty, contains a non-digit, or is shorter than 6 / longer than 19 digits.
    /// </exception>
    Task<BinClassificationResult> ClassifyAsync(
        string bin, decimal? amount = null, string? amountCurrency = null,
        CancellationToken cancellationToken = default);
}
