namespace GastroGestion.Domain.Enums;

/// <summary>
/// Domain roles referenced by the state-machine transition registry.
/// Authorization policy mapping (role → named policy) is configured in phase 5 (API layer).
/// </summary>
public enum RolUsuario
{
    Administrador = 0,
    Cajero        = 1,
    Mozo          = 2,
    Cocinero      = 3
}
