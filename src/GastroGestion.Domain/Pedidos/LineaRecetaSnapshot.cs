using GastroGestion.Domain.Enums;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Domain.Pedidos;

/// <summary>
/// Immutable snapshot of a single recipe line captured at OT creation.
/// Records the exact ingredient and quantity as they were when the OT was
/// generated, so stock restoration and auditing are correct even if the
/// Plato recipe is later modified (design §7, functional-scope §3 improvement).
/// </summary>
/// <param name="IngredienteId">Cross-boundary Id of the ingredient.</param>
/// <param name="Cantidad">Quantity consumed by this recipe line.</param>
public sealed record LineaRecetaSnapshot(
    Guid IngredienteId,
    Cantidad Cantidad);
