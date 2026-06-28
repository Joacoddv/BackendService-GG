using GastroGestion.Domain.Enums;

namespace GastroGestion.Application.Clientes.CrearCliente;

public sealed record CrearClienteCommand(
    string Nombre,
    CondicionIVA CondicionIVA,
    string? Cuit,
    string? Email,
    DateOnly? FechaNacimiento = null,
    string? Apellido = null,
    string? Telefono = null,
    string? Dni = null);
