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

        RuleFor(x => x.Dni)
            .MaximumLength(20).When(x => x.Dni is not null);
    }
}

public sealed class EditarClienteValidator : AbstractValidator<EditarClienteRequest>
{
    public EditarClienteValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("Nombre is required.");

        // RI-requires-CUIT invariant is enforced by the domain (ActualizarDatos → DomainException → 422).

        RuleFor(x => x.Dni)
            .MaximumLength(20).When(x => x.Dni is not null);
    }
}
