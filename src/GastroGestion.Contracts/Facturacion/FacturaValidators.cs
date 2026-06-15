using FluentValidation;

namespace GastroGestion.Contracts.Facturacion;

public sealed class CrearFacturaValidator : AbstractValidator<CrearFacturaRequest>
{
    public CrearFacturaValidator()
    {
        RuleFor(x => x.ClienteId)
            .NotEmpty().WithMessage("ClienteId is required.");

        RuleFor(x => x.PedidoIds)
            .NotEmpty().WithMessage("At least one PedidoId is required.");
    }
}

public sealed class RegistrarPagoValidator : AbstractValidator<RegistrarPagoRequest>
{
    public RegistrarPagoValidator()
    {
        RuleFor(x => x.Monto)
            .GreaterThan(0).WithMessage("Monto must be greater than zero.");
    }
}
