using FluentValidation;

namespace GastroGestion.Contracts.Platos;

public sealed class PlatoValidator : AbstractValidator<CrearPlatoRequest>
{
    public PlatoValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("Nombre is required.");

        RuleFor(x => x.PrecioBase)
            .GreaterThan(0).WithMessage("PrecioBase must be greater than zero.");
    }
}

/// <summary>Validator for PUT /platos/{id}.</summary>
public sealed class EditarPlatoValidator : AbstractValidator<EditarPlatoRequest>
{
    public EditarPlatoValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("Nombre is required.");

        RuleFor(x => x.PrecioBase)
            .GreaterThan(0).WithMessage("PrecioBase must be greater than zero.");
    }
}
