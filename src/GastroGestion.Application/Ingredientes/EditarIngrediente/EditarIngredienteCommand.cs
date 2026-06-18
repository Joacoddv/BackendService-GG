namespace GastroGestion.Application.Ingredientes.EditarIngrediente;

/// <summary>
/// Command to update an ingrediente's Nombre.
/// UnidadBase is intentionally absent — it is immutable after creation (ADR-CCC-1).
/// </summary>
/// <param name="Id">Primary key of the ingrediente to update.</param>
/// <param name="Nombre">New display name — must be non-empty.</param>
public sealed record EditarIngredienteCommand(Guid Id, string Nombre);
