using FluentValidation;

namespace GastroGestion.Contracts.Mesas;

public sealed class MesaValidator : AbstractValidator<CrearMesaRequest>
{
    public MesaValidator()
    {
        RuleFor(x => x.Numero)
            .GreaterThan(0).WithMessage("Numero must be greater than zero.");

        RuleFor(x => x.Capacidad)
            .GreaterThan(0).WithMessage("Capacidad must be greater than zero.");
    }
}

/// <summary>Validator for PUT /mesas/{id}.</summary>
public sealed class EditarMesaValidator : AbstractValidator<EditarMesaRequest>
{
    public EditarMesaValidator()
    {
        RuleFor(x => x.Numero)
            .GreaterThan(0).WithMessage("Numero must be greater than zero.");

        RuleFor(x => x.Capacidad)
            .GreaterThan(0).WithMessage("Capacidad must be greater than zero.");
    }
}
