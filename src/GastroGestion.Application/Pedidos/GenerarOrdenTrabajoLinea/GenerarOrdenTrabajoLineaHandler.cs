using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Domain.Pedidos;

namespace GastroGestion.Application.Pedidos.GenerarOrdenTrabajoLinea;

/// <summary>
/// Generates the kitchen work order for a single (newly added, already priced) line. Used when a
/// line is added to an order whose OTs were already generated — the all-or-nothing batch generator
/// cannot be re-run. Resolves the plato's recipe snapshot and delegates to the aggregate.
/// </summary>
public sealed class GenerarOrdenTrabajoLineaHandler
{
    private readonly IPedidoRepository _pedidos;
    private readonly IPlatoRepository  _platos;
    private readonly IUnitOfWork       _uow;

    public GenerarOrdenTrabajoLineaHandler(
        IPedidoRepository pedidos,
        IPlatoRepository platos,
        IUnitOfWork uow)
    {
        _pedidos = pedidos;
        _platos  = platos;
        _uow     = uow;
    }

    public async Task Handle(GenerarOrdenTrabajoLineaCommand cmd, CancellationToken ct = default)
    {
        var pedido = await _pedidos.GetByIdAsync(cmd.PedidoId, ct)
            ?? throw new NotFoundException($"Pedido {cmd.PedidoId} not found.");

        var linea = pedido.Lineas.FirstOrDefault(l => l.Id == cmd.LineaId)
            ?? throw new NotFoundException($"LineaPedido {cmd.LineaId} not found.");

        var plato = await _platos.GetByIdAsync(linea.PlatoId, ct)
            ?? throw new ValidationException($"Plato {linea.PlatoId} was not found; cannot generate the OT.");

        if (plato.LineasReceta.Count == 0)
            throw new ValidationException($"Plato {linea.PlatoId} has no recipe lines; cannot generate the OT.");

        var receta = (IReadOnlyList<LineaRecetaSnapshot>)plato.LineasReceta
            .Select(lr => new LineaRecetaSnapshot(lr.IngredienteId, lr.Cantidad))
            .ToList()
            .AsReadOnly();

        pedido.GenerarOrdenTrabajoParaLinea(linea.Id, receta);

        await _uow.SaveChangesAsync(ct);
    }
}
