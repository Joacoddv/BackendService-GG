namespace GastroGestion.Application.Platos.EditarPlato;

/// <summary>
/// Command to update a plato's Nombre and PrecioBase.
/// Recipe-line editing is out of scope for this command.
/// </summary>
/// <param name="Id">Primary key of the plato to update.</param>
/// <param name="Nombre">New display name — must be non-empty.</param>
/// <param name="PrecioBase">New base price — must be non-negative.</param>
public sealed record EditarPlatoCommand(Guid Id, string Nombre, decimal PrecioBase);
