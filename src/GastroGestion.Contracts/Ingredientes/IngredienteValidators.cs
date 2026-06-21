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

/// <summary>
/// Validator for PUT /ingredientes/{id}. Only Nombre is validated — UnidadBase is absent from the DTO.
/// </summary>
public sealed class EditarIngredienteValidator : AbstractValidator<EditarIngredienteRequest>
{
    public EditarIngredienteValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("Nombre is required.");
    }
}

/// <summary>Validator for PUT /ingredientes/{id}/stock-minimo.</summary>
public sealed class ActualizarStockMinimoValidator : AbstractValidator<ActualizarStockMinimoRequest>
{
    public ActualizarStockMinimoValidator()
    {
        RuleFor(x => x.StockMinimo)
            .GreaterThanOrEqualTo(0).WithMessage("StockMinimo must be zero or positive.");
    }
}
