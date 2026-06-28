namespace GastroGestion.Application.Menus.EditarMenu;

/// <summary>
/// Command to update a menu's Nombre and FechaVigencia.
/// Item editing is out of scope for this command.
/// </summary>
/// <param name="Id">Primary key of the menu to update.</param>
/// <param name="Nombre">New display name — must be non-empty.</param>
/// <param name="FechaVigencia">New effective date — must be in the future.</param>
public sealed record EditarMenuCommand(Guid Id, string Nombre, DateOnly FechaVigencia);
