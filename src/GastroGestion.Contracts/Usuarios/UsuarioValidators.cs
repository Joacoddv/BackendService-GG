using FluentValidation;

namespace GastroGestion.Contracts.Usuarios;

public sealed class CrearUsuarioRequestValidator : AbstractValidator<CrearUsuarioRequest>
{
    public CrearUsuarioRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("Email must be a valid email address.");

        RuleFor(x => x.NombreCompleto)
            .NotEmpty().WithMessage("NombreCompleto is required.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters.");

        RuleFor(x => x.Rol)
            .IsInEnum().WithMessage("Rol must be a valid role.");
    }
}

public sealed class EditarUsuarioRequestValidator : AbstractValidator<EditarUsuarioRequest>
{
    public EditarUsuarioRequestValidator()
    {
        RuleFor(x => x.NombreCompleto)
            .NotEmpty().WithMessage("NombreCompleto is required.");

        RuleFor(x => x.Rol)
            .IsInEnum().WithMessage("Rol must be a valid role.");
    }
}
