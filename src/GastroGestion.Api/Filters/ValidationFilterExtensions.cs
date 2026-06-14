namespace GastroGestion.Api.Filters;

/// <summary>
/// Extension methods for wiring <see cref="ValidationFilter{T}"/> to Minimal API endpoints.
/// </summary>
public static class ValidationFilterExtensions
{
    /// <summary>
    /// Adds <see cref="ValidationFilter{T}"/> to the endpoint so FluentValidation runs
    /// before the handler and short-circuits with 400 ValidationProblem on failure.
    /// </summary>
    public static RouteHandlerBuilder WithValidation<T>(this RouteHandlerBuilder builder)
        where T : class
        => builder.AddEndpointFilter<ValidationFilter<T>>();
}
