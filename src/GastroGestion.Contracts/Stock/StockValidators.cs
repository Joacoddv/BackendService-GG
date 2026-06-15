using FluentValidation;

namespace GastroGestion.Contracts.Stock;

public sealed class RegistrarMovimientoStockValidator : AbstractValidator<RegistrarMovimientoStockRequest>
{
    public RegistrarMovimientoStockValidator()
    {
        RuleFor(x => x.IngredienteId)
            .NotEmpty().WithMessage("IngredienteId is required.");

        // Caller passes absolute value; factory applies sign convention
        RuleFor(x => x.Cantidad)
            .GreaterThan(0).WithMessage("Cantidad must be greater than zero (pass absolute value; sign is applied by the domain).");
    }
}
