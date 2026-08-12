using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;

namespace BinTool.Api.Errors;

public sealed class ApiExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(
        IProblemDetailsService problemDetails, ILogger<ApiExceptionHandler> logger)
    {
        _problemDetails = problemDetails;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext context, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title, detail) = Describe(exception);

        if (status == StatusCodes.Status500InternalServerError)
        {
            ApiErrorLog.Unhandled(_logger, exception, context.Request.Method, context.Request.Path);
        }
        else
        {
            ApiErrorLog.Refused(_logger, context.Request.Method, context.Request.Path, detail);
        }

        context.Response.StatusCode = status;

        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = title,
                Detail = detail
            }
        });
    }

    // The detail is echoed to the caller, so only messages this codebase wrote are used.
    private static (int Status, string Title, string Detail) Describe(Exception exception) =>
        exception switch
        {
            ArgumentException e => (
                StatusCodes.Status400BadRequest,
                "The request could not be accepted.",
                e.Message),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Something went wrong.",
                "The request could not be completed. The failure has been logged.")
        };
}
