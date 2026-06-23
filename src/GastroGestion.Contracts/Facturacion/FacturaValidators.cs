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

public sealed class AnularFacturaValidator : AbstractValidator<AnularFacturaRequest>
{
    public AnularFacturaValidator()
    {
        RuleFor(x => x.Motivo)
            .NotEmpty().WithMessage("Motivo is required.");
    }
}

public sealed class AsignarCaeValidator : AbstractValidator<AsignarCaeRequest>
{
    public AsignarCaeValidator()
    {
        RuleFor(x => x.Cae)
            .NotEmpty().WithMessage("Cae is required.")
            .MaximumLength(14).WithMessage("Cae must not exceed 14 characters.");

        RuleFor(x => x.Vencimiento)
            .Must(v => v != default).WithMessage("Vencimiento is required.");
    }
}
