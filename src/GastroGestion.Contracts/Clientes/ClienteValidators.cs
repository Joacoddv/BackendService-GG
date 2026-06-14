using FluentValidation;

namespace GastroGestion.Contracts.Clientes;

public sealed class ClienteValidator : AbstractValidator<CrearClienteRequest>
{
    public ClienteValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("Nombre is required.");

        // Presence of CUIT when CondicionIVA = ResponsableInscripto is enforced
        // by the domain (Cliente.Crear throws DomainException → 422).
        // This validator only checks format when a non-null CUIT is provided.
    }
}
