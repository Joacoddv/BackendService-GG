using GastroGestion.Domain.Enums;

namespace GastroGestion.Contracts.Clientes;

/// <summary>
/// Request DTO for creating a new Cliente.
/// Used by POST /clientes.
/// </summary>
/// <param name="Nombre">Display name — required, non-empty.</param>
/// <param name="CondicionIVA">Fiscal condition.</param>
/// <param name="Cuit">Required when CondicionIVA = ResponsableInscripto.</param>
/// <param name="Email">Optional contact email.</param>
public sealed record CrearClienteRequest(
    string Nombre,
    CondicionIVA CondicionIVA,
    string? Cuit,
    string? Email,
    DateOnly? FechaNacimiento = null);

/// <summary>
/// Request DTO for editing an existing Cliente.
/// Used by PUT /clientes/{id}. NumeroCliente is not included — it is immutable.
/// </summary>
/// <param name="Nombre">Display name — required, non-empty.</param>
/// <param name="CondicionIVA">Fiscal condition.</param>
/// <param name="Cuit">Required when CondicionIVA = ResponsableInscripto.</param>
/// <param name="Email">Optional contact email.</param>
public sealed record EditarClienteRequest(
    string Nombre,
    CondicionIVA CondicionIVA,
    string? Cuit,
    string? Email,
    DateOnly? FechaNacimiento = null);

/// <summary>Request DTO for POST /clientes/{id}/direcciones.</summary>
public sealed record AgregarDireccionRequest(
    string Calle,
    string Numero,
    string Ciudad,
    string Provincia,
    string CodigoPostal,
    string? Piso,
    string? Departamento);
