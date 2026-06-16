using FluentValidation;

namespace GastroGestion.Contracts.Auth;

/// <summary>
/// Validates LoginRequest before the handler is invoked.
/// Wired via WithValidation&lt;LoginRequest&gt;() on the endpoint (AUTH-05.4).
/// </summary>
public sealed class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty();
    }
}
