using FluentValidation;
using GastroGestion.Domain.Enums;

namespace GastroGestion.Contracts.Pedidos;

public sealed class CrearPedidoValidator : AbstractValidator<CrearPedidoRequest>
{
    public CrearPedidoValidator()
    {
        RuleFor(x => x.MesaId)
            .NotEmpty()
            .When(x => x.Tipo == TipoPedido.Salon)
            .WithMessage("MesaId is required for Salon orders.");
    }
}

public sealed class AgregarLineaValidator : AbstractValidator<AgregarLineaRequest>
{
    public AgregarLineaValidator()
    {
        RuleFor(x => x.PlatoId)
            .NotEmpty().WithMessage("PlatoId is required.");

        RuleFor(x => x.Cantidad)
            .GreaterThan(0).WithMessage("Cantidad must be greater than zero.");
    }
}

public sealed class ActualizarLineaValidator : AbstractValidator<ActualizarLineaRequest>
{
    public ActualizarLineaValidator()
    {
        RuleFor(x => x.Cantidad)
            .GreaterThan(0).WithMessage("Cantidad must be greater than zero.");
    }
}
