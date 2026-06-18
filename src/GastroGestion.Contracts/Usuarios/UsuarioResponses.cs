namespace GastroGestion.Contracts.Usuarios;

/// <summary>
/// Response DTO for a Cocinero user exposed via GET /usuarios/cocineros (CCC-A01).
/// </summary>
public sealed record CocineroResponse(Guid Id, string NombreCompleto);
