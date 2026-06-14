using FluentValidation;
using GastroGestion.Domain.Enums;

namespace GastroGestion.Contracts.Clientes;

public sealed class ClienteValidator : AbstractValidator<CrearClienteRequest>
{
    public ClienteValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("Nombre is required.");

        RuleFor(x => x.Cuit)
            .NotEmpty().WithMessage("CUIT is required for ResponsableInscripto.")
            .When(x => x.CondicionIVA == CondicionIVA.ResponsableInscripto);
    }
}
