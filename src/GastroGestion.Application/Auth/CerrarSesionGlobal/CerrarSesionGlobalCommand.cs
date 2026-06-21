namespace GastroGestion.Application.Auth.CerrarSesionGlobal;

/// <summary>Command for POST /auth/logout-all — revokes every active session of the user.</summary>
public sealed record CerrarSesionGlobalCommand(Guid UsuarioId);
