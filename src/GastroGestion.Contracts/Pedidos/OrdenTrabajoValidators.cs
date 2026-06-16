using FluentValidation;

namespace GastroGestion.Contracts.Pedidos;

public sealed class AsignarCocineroRequestValidator : AbstractValidator<AsignarCocineroRequest>
{
    public AsignarCocineroRequestValidator()
    {
        RuleFor(x => x.CocineroLegajoId).NotEmpty();
    }
}
