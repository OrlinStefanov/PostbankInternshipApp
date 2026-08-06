using BinTool.Core.Authorization;
using BinTool.Core.Models.Classification;
using BinTool.Core.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.Api.Controllers;

/// <summary>
/// Classifies card BINs against the stored BIN ranges.
/// <para>
/// Requires the <c>bin.classify</c> permission. Classification only reads, so any role granted
/// it - Viewer holds it by default - can classify.
/// </para>
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
[Authorize(Policy = Permissions.BinClassify)]
public class BinController : ControllerBase
{
    private readonly IBinClassificationService _service;

    public BinController(IBinClassificationService service)
    {
        _service = service;
    }

    /// <summary>
    /// Classifies a BIN.
    /// </summary>
    /// <remarks>
    /// Sample request:
    ///
    ///     POST /api/bin/classify
    ///     { "bin": "400001", "amount": 100 }
    ///
    /// The lookup is a longest-prefix match. An 8-digit range is more specific than the
    /// 6-digit range it sits inside, so it wins; if no 8-digit range covers the BIN the
    /// 7-digit one is tried, then the 6-digit one. Only ranges that are valid today are
    /// considered - a range that has expired, has not started yet, or has been deleted
    /// is skipped, and a shorter range may then match in its place.
    ///
    /// A full card number may be sent instead of a BIN. Only the leading 8 digits are
    /// used: the rest is discarded before the lookup runs, is never stored or logged,
    /// and the response echoes back only the truncated value.
    ///
    /// When an <c>amount</c> is supplied and the BIN matches, the response also carries the
    /// commission: the resolved rule (or the default, flagged as a fallback) and the fee.
    ///
    /// No scheme ranges are hard-coded. Everything the response reports comes from the
    /// BIN ranges and reference data in the database, so an import changes the answer.
    /// </remarks>
    /// <param name="request">The BIN to classify.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <response code="200">
    /// The classification ran. Check <c>matched</c> - when no range covers the BIN the
    /// response is still 200, with <c>matched: false</c> and the card attributes null.
    /// </response>
    /// <response code="400">The BIN is missing, contains a non-digit, or is not 6-19 digits long.</response>
    [HttpPost("classify")]
    [ProducesResponseType(typeof(BinClassificationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Classify(
        [FromBody] BinClassificationRequest request, CancellationToken cancellationToken)
    {
        var result = await _service.ClassifyAsync(request.Bin, request.Amount, cancellationToken);

        return Ok(result);
    }
}
