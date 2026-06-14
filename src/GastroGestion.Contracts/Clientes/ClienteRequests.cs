using GastroGestion.Domain.Enums;

namespace GastroGestion.Contracts.Clientes;

/// <summary>
/// Request DTO for creating a new Cliente.
/// Used by POST /clientes (Phase 4 — PR 2 wires the endpoint).
/// </summary>
/// <param name="Nombre">Display name — required, non-empty.</param>
/// <param name="CondicionIVA">Fiscal condition.</param>
/// <param name="Cuit">Required when CondicionIVA = ResponsableInscripto.</param>
/// <param name="Email">Optional contact email.</param>
public sealed record CrearClienteRequest(
    string Nombre,
    CondicionIVA CondicionIVA,
    string? Cuit,
    string? Email);
