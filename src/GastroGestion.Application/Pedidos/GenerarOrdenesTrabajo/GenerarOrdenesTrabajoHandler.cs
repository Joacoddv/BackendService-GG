using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Pedidos;

namespace GastroGestion.Application.Pedidos.GenerarOrdenesTrabajo;

public sealed class GenerarOrdenesTrabajoHandler
{
    private readonly IPedidoRepository         _pedidos;
    private readonly IPlatoRepository          _platos;
    private readonly IMovimientoStockRepository _stock;
    private readonly IUnitOfWork               _uow;

    public GenerarOrdenesTrabajoHandler(
        IPedidoRepository pedidos,
        IPlatoRepository  platos,
        IMovimientoStockRepository stock,
        IUnitOfWork       uow)
    {
        _pedidos = pedidos;
        _platos  = platos;
        _stock   = stock;
        _uow     = uow;
    }

    public async Task Handle(GenerarOrdenesTrabajoCommand cmd, CancellationToken ct = default)
    {
        var pedido = await _pedidos.GetByIdAsync(cmd.PedidoId, ct)
            ?? throw new NotFoundException($"Pedido {cmd.PedidoId} not found.");

        // Role gate — only Mozo and Administrador may generate work orders
        if (cmd.Rol is not (RolUsuario.Mozo or RolUsuario.Administrador))
            throw new ForbiddenException(
                $"Role {cmd.Rol} is not allowed to generate work orders. Required: Mozo or Administrador.");

        // Conflict guard — re-generation is forbidden (OT-01-E → HTTP 409)
        if (pedido.OrdenesTrabajo.Any())
            throw new ConflictException("Work orders already exist for this Pedido.");

        // Pre-validate confirmed prices (OT-01-B → HTTP 422)
        var unpricedLines = pedido.Lineas
            .Where(l => l.PrecioUnitario is null)
            .Select(l => l.Id)
            .ToList();
        if (unpricedLines.Count > 0)
            throw new ValidationException(
                $"The following LineaPedido entries have no confirmed price and cannot generate OTs: " +
                string.Join(", ", unpricedLines));

        // Batch-load Platos for recipe resolution (OT-01-C)
        var distinctPlatoIds = pedido.Lineas
            .Select(l => l.PlatoId)
            .Distinct()
            .ToList();

        var platosLoaded = await _platos.GetByIdsAsync(distinctPlatoIds, ct);
        var platoMap = platosLoaded.ToDictionary(p => p.Id);

        // Early-fail if any Plato is missing or has empty LineasReceta
        foreach (var platoId in distinctPlatoIds)
        {
            if (!platoMap.TryGetValue(platoId, out var plato))
                throw new ValidationException(
                    $"Plato {platoId} was not found; cannot generate OTs.");

            if (plato.LineasReceta.Count == 0)
                throw new ValidationException(
                    $"Plato {platoId} has no recipe lines; cannot generate OTs.");
        }

        // Build the snapshot dictionary keyed by PlatoId
        var snapshotsByPlato = platoMap.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyList<LineaRecetaSnapshot>)kvp.Value.LineasReceta
                .Select(lr => new LineaRecetaSnapshot(lr.IngredienteId, lr.Cantidad))
                .ToList()
                .AsReadOnly());

        // Stock guard — block generation if any recipe ingredient is short (OT creation reserves
        // stock). Required per ingredient = sum of the per-dish recipe quantity across every line
        // (one OT per line), matching what the reservation handler will consume.
        await GuardStockSuficienteAsync(pedido, snapshotsByPlato, ct);

        // Domain: all-or-nothing generation + OrdenTrabajoCreada event dispatch
        pedido.GenerarOrdenesTrabajo(snapshotsByPlato);

        await _uow.SaveChangesAsync(ct);
    }

    private async Task GuardStockSuficienteAsync(
        Pedido pedido,
        IReadOnlyDictionary<Guid, IReadOnlyList<LineaRecetaSnapshot>> snapshotsByPlato,
        CancellationToken ct)
    {
        // Aggregate required quantity per ingredient across all lines.
        var requeridoPorIngrediente = new Dictionary<Guid, decimal>();
        foreach (var linea in pedido.Lineas)
        {
            if (!snapshotsByPlato.TryGetValue(linea.PlatoId, out var receta))
                continue;
            foreach (var item in receta)
            {
                requeridoPorIngrediente.TryGetValue(item.IngredienteId, out var acc);
                requeridoPorIngrediente[item.IngredienteId] = acc + item.Cantidad.Valor;
            }
        }

        var faltantes = new List<string>();
        foreach (var (ingredienteId, requerido) in requeridoPorIngrediente)
        {
            var disponible = await _stock.CalcularBalanceAsync(ingredienteId, ct);
            if (disponible < requerido)
                faltantes.Add($"{ingredienteId} (necesita {requerido}, disponible {disponible})");
        }

        if (faltantes.Count > 0)
            throw new ConflictException(
                "Stock insuficiente para generar las órdenes de trabajo: " + string.Join("; ", faltantes));
    }
}
