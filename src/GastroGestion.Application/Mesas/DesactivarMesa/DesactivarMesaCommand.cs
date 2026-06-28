namespace GastroGestion.Application.Mesas.DesactivarMesa;

/// <summary>Command to soft-delete a mesa by id.</summary>
/// <param name="Id">Primary key of the mesa to deactivate.</param>
public sealed record DesactivarMesaCommand(Guid Id);
