using BinTool.Application.Models.Common;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.Api.Errors;

public static class ApiResults
{
    /// <summary>The result as its HTTP answer.</summary>
    public static IActionResult From(IMutationResult result) => result.Outcome switch
    {
        MutationOutcome.NotFound => new NotFoundObjectResult(result),
        MutationOutcome.Invalid => new BadRequestObjectResult(result),
        MutationOutcome.Conflict => new ConflictObjectResult(result),
        _ => new OkObjectResult(result)
    };

    /// <summary>
    /// A request refused before any service was asked - a missing file, an absent body.
    /// </summary>
    public static IActionResult Invalid(string detail) =>
        new BadRequestObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "The request could not be accepted.",
            Detail = detail
        });
}
