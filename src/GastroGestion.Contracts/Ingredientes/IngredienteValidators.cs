using FluentValidation;

namespace GastroGestion.Contracts.Ingredientes;

public sealed class IngredienteValidator : AbstractValidator<CrearIngredienteRequest>
{
    public IngredienteValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("Nombre is required.");
    }
}
