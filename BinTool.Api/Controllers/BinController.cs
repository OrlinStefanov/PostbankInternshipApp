using BinTool.Application.Abstractions;
using BinTool.Application.Authorization;
using BinTool.Application.Models.Classification;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.Api.Controllers;

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
