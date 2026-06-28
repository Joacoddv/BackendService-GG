namespace GastroGestion.Application.Menus.DesactivarMenu;

/// <summary>Command to soft-delete a menu by id.</summary>
/// <param name="Id">Primary key of the menu to deactivate.</param>
public sealed record DesactivarMenuCommand(Guid Id);
