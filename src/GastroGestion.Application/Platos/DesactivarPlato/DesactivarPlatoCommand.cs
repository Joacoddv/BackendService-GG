namespace GastroGestion.Application.Platos.DesactivarPlato;

/// <summary>Command to soft-delete a plato by id.</summary>
/// <param name="Id">Primary key of the plato to deactivate.</param>
public sealed record DesactivarPlatoCommand(Guid Id);
