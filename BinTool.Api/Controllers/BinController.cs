using BinTool.Application.Abstractions;
using BinTool.Application.Authorization;
using BinTool.Application.Models.Classification;
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

    /// <summary>Classifies a BIN.</summary>
    /// <param name="request">The BIN to classify.</param>
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
        var result = await _service.ClassifyAsync(
            request.Bin, request.Amount, request.AmountCurrency, cancellationToken);

        return Ok(result);
    }
}
