using FluentValidation;

namespace GastroGestion.Contracts.Menus;

public sealed class MenuValidator : AbstractValidator<CrearMenuRequest>
{
    public MenuValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("Nombre is required.");

        RuleFor(x => x.FechaVigencia)
            .GreaterThan(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("FechaVigencia must be a future date.");
    }
}

/// <summary>Validator for PUT /menus/{id}.</summary>
public sealed class EditarMenuValidator : AbstractValidator<EditarMenuRequest>
{
    public EditarMenuValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("Nombre is required.");

        RuleFor(x => x.FechaVigencia)
            .GreaterThan(DateOnly.FromDateTime(DateTime.UtcNow))
            .WithMessage("FechaVigencia must be a future date.");
    }
}
