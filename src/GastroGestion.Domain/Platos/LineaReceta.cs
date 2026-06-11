using GastroGestion.Domain.Common;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Domain.Platos;

/// <summary>
/// A single ingredient line in a <see cref="Plato"/> recipe.
/// Owned by <see cref="Plato"/>; has identity (EF Core owned entity with shadow key).
/// <para>
/// <see cref="PlatoReferenciadoId"/> is a nullable sub-recipe seam: null in v1
/// (a line references an <see cref="GastroGestion.Domain.Ingredientes.Ingrediente"/>);
/// a future version may allow a line to reference another <see cref="Plato"/> for combos.
/// </para>
/// </summary>
public class LineaReceta : Entity
{
    /// <summary>Cross-boundary reference to the ingredient (by Id only).</summary>
    public Guid IngredienteId { get; private set; }

    /// <summary>Required quantity of the ingredient in this recipe line.</summary>
    public Cantidad Cantidad { get; private set; }

    /// <summary>
    /// Sub-recipe seam: null in v1. When non-null, this line references a composite
    /// Plato rather than a raw Ingrediente.
    /// </summary>
    public Guid? PlatoReferenciadoId { get; private set; }

    public LineaReceta(Guid id, Guid ingredienteId, Cantidad cantidad, Guid? platoReferenciadoId = null)
        : base(id)
    {
        IngredienteId     = ingredienteId;
        Cantidad          = cantidad;
        PlatoReferenciadoId = platoReferenciadoId;
    }

    // EF Core parameterless ctor.
#pragma warning disable CS8618
    protected LineaReceta() { }
#pragma warning restore CS8618
}
