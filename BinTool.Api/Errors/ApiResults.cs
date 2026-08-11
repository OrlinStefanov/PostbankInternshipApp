using BinTool.Application.Models.Common;
using Microsoft.AspNetCore.Mvc;

namespace BinTool.Api.Errors;

/// <summary>
/// The one place a refused write becomes a status code.
/// <para>
/// Every controller used to carry its own switch over its own status enum - seven copies of
/// the same table, and each new status value was a chance to forget one and answer 200 to a
/// refusal. The status enums stay where they are, because which rule refused is what the
/// message on screen is made of; what moved here is the part that was never feature-specific.
/// </para>
/// </summary>
public static class ApiResults
{
    /// <summary>
    /// The result as its HTTP answer. The body is the result itself either way, so a client
    /// reads one shape whether the write succeeded or was refused, and its <c>Error</c> is
    /// there to show.
    /// </summary>
    public static IActionResult From(IMutationResult result) => result.Outcome switch
    {
        MutationOutcome.NotFound => new NotFoundObjectResult(result),
        MutationOutcome.Invalid => new BadRequestObjectResult(result),
        MutationOutcome.Conflict => new ConflictObjectResult(result),
        _ => new OkObjectResult(result)
    };

    /// <summary>
    /// A request refused before any service was asked - a missing file, an absent body. The
    /// body is ProblemDetails, the same shape [ApiController] produces for a model-binding
    /// failure and the exception handler produces for anything that escapes an action, so a
    /// client has one error shape to read rather than three.
    /// </summary>
    /// <remarks>
    /// Built here rather than through <c>ControllerBase.ValidationProblem</c>, which leaves
    /// the status code for a factory to fill in later and so reads as null to anything
    /// holding the result directly.
    /// </remarks>
    public static IActionResult Invalid(string detail) =>
        new BadRequestObjectResult(new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "The request could not be accepted.",
            Detail = detail
        });
}
