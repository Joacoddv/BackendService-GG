namespace GastroGestion.Application.Ingredientes.BuscarIngredientes;

/// <summary>Query to search/list ingredientes with optional name filter and active toggle.</summary>
/// <param name="Nombre">Optional partial name filter (case-insensitive).</param>
/// <param name="IncluirInactivos">When true, inactive ingredientes are included in results.</param>
public sealed record BuscarIngredientesQuery(string? Nombre, bool IncluirInactivos);
