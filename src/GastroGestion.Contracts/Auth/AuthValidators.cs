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

/// <summary>Validates RefrescarTokenRequest. Wired via WithValidation&lt;RefrescarTokenRequest&gt;().</summary>
public sealed class RefrescarTokenValidator : AbstractValidator<RefrescarTokenRequest>
{
    public RefrescarTokenValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty();
    }
}
