using System.Text.Json;
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
            AuthenticationFailedException => (StatusCodes.Status401Unauthorized,
                                               "Authentication failed",
                                               "Invalid credentials."),
            ForbiddenException ex  => (StatusCodes.Status403Forbidden,
                                       "Forbidden",
                                       ex.Message),
            ConflictException ex  => (StatusCodes.Status409Conflict,
                                      "Business rule conflict",
                                      ex.Message),
            NotFoundException ex  => (StatusCodes.Status404NotFound,
                                      "Resource not found",
                                      ex.Message),
            ValidationException ex => (StatusCodes.Status422UnprocessableEntity,
                                       "Validation failed",
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

        // Set status code and RFC 7807-compliant Content-Type BEFORE writing the body.
        // WriteAsJsonAsync would default to application/json, so we write manually instead.
        httpContext.Response.StatusCode  = statusCode;
        httpContext.Response.ContentType = "application/problem+json";
        var json = JsonSerializer.Serialize(problem);
        await httpContext.Response.WriteAsync(json, cancellationToken);

        return true;
    }
}
