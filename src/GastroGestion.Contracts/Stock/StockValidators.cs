using FluentValidation;
using GastroGestion.Domain.Enums;

namespace GastroGestion.Contracts.Stock;

public sealed class RegistrarMovimientoStockValidator : AbstractValidator<RegistrarMovimientoStockRequest>
{
    // Only human-initiated movement types may be posted through the API. Reserva / Consumo /
    // LiberacionReserva / DevolucionCancelacion are driven exclusively by the OT lifecycle event
    // handlers; allowing them here would corrupt the reservation/consumption accounting.
    private static readonly TipoMovimientoStock[] TiposManuales =
    {
        TipoMovimientoStock.Compra,
        TipoMovimientoStock.Ajuste,
        TipoMovimientoStock.Merma
    };

    public RegistrarMovimientoStockValidator()
    {
        RuleFor(x => x.IngredienteId)
            .NotEmpty().WithMessage("IngredienteId is required.");

        // Caller passes absolute value; factory applies sign convention
        RuleFor(x => x.Cantidad)
            .GreaterThan(0).WithMessage("Cantidad must be greater than zero (pass absolute value; sign is applied by the domain).");

        RuleFor(x => x.Tipo)
            .Must(t => TiposManuales.Contains(t))
            .WithMessage("Only Compra, Ajuste or Merma can be registered manually; reservation/consumption movements are system-driven.");
    }
}
