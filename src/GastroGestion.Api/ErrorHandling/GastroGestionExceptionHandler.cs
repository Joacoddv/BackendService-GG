using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace GastroGestion.Api.ErrorHandling;

/// <summary>
/// Maps domain and application exceptions to RFC 7807 ProblemDetails responses.
/// Registered via AddExceptionHandler&lt;GastroGestionExceptionHandler&gt;() + AddProblemDetails().
/// Must be first in the middleware pipeline: app.UseExceptionHandler().
/// </summary>
internal sealed class GastroGestionExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GastroGestionExceptionHandler> _logger;

    public GastroGestionExceptionHandler(ILogger<GastroGestionExceptionHandler> logger)
        => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = exception switch
        {
            ConflictException ex  => (StatusCodes.Status409Conflict,
                                      "Business rule conflict",
                                      ex.Message),
            NotFoundException ex  => (StatusCodes.Status404NotFound,
                                      "Resource not found",
                                      ex.Message),
            DomainException ex    => (StatusCodes.Status422UnprocessableEntity,
                                      "Domain rule violation",
                                      ex.Message),
            _                     => (StatusCodes.Status500InternalServerError,
                                      "An unexpected error occurred",
                                      "An internal server error has occurred.")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Unhandled exception caught by GastroGestionExceptionHandler");

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title  = title,
            Detail = detail
        };

        httpContext.Response.StatusCode = statusCode;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken);

        return true;
    }
}
