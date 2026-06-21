namespace GastroGestion.Infrastructure.Persistence.Configurations;

/// <summary>
/// Marker for EF configurations that belong to the SeguridadDbContext (Usuario, RefreshToken).
/// Lets each context apply only its own configurations from the shared assembly:
/// the main context excludes these, the security context includes only these.
/// </summary>
internal interface ISecurityEntityTypeConfiguration;
