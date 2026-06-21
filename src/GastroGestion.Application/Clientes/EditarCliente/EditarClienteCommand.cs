using GastroGestion.Domain.Enums;

namespace GastroGestion.Application.Clientes.EditarCliente;

public sealed record EditarClienteCommand(
    Guid Id,
    string Nombre,
    CondicionIVA CondicionIVA,
    string? Cuit,
    string? Email,
    DateOnly? FechaNacimiento = null);
