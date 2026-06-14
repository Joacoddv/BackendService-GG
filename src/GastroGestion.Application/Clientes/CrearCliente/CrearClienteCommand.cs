using GastroGestion.Domain.Enums;

namespace GastroGestion.Application.Clientes.CrearCliente;

public sealed record CrearClienteCommand(
    string Nombre,
    CondicionIVA CondicionIVA,
    string? Cuit,
    string? Email);
