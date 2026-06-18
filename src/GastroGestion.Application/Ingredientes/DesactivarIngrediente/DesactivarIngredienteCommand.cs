namespace GastroGestion.Application.Ingredientes.DesactivarIngrediente;

/// <summary>Command to soft-delete an ingrediente by id.</summary>
/// <param name="Id">Primary key of the ingrediente to deactivate.</param>
public sealed record DesactivarIngredienteCommand(Guid Id);
