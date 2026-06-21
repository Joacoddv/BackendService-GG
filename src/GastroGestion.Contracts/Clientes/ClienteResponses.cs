using GastroGestion.Domain.Enums;

namespace GastroGestion.Contracts.Clientes;

public sealed record ClienteResponse(
    Guid Id,
    string Nombre,
    CondicionIVA CondicionIVA,
    string? Cuit,
    string? Email,
    bool Activo,
    DateOnly? FechaNacimiento,
    IReadOnlyList<DireccionResponse> Direcciones);

public sealed record DireccionResponse(
    Guid Id,
    string Calle,
    string Numero,
    string Ciudad,
    string Provincia,
    string CodigoPostal,
    string? Piso,
    string? Departamento);
