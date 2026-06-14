using FluentValidation;

namespace GastroGestion.Api.Filters;

/// <summary>
/// Generic endpoint filter that runs FluentValidation before the handler executes.
/// Short-circuits with a 400 ValidationProblem when the request is invalid.
/// Apply via the <see cref="ValidationFilterExtensions.WithValidation{T}"/> extension.
/// </summary>
public sealed class ValidationFilter<T> : IEndpointFilter where T : class
{
    private readonly IValidator<T> _validator;

    public ValidationFilter(IValidator<T> validator) => _validator = validator;

    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var arg = context.Arguments.OfType<T>().FirstOrDefault();
        if (arg is null)
            return await next(context);

        var result = await _validator.ValidateAsync(arg, context.HttpContext.RequestAborted);
        if (!result.IsValid)
            return TypedResults.ValidationProblem(result.ToDictionary());

        return await next(context);
    }
}
