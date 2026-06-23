using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;

namespace GastroGestion.Application.Facturacion.AsignarCae;

public sealed class AsignarCaeHandler
{
    private readonly IFacturaRepository _facturas;
    private readonly IUnitOfWork        _uow;

    public AsignarCaeHandler(IFacturaRepository facturas, IUnitOfWork uow)
    {
        _facturas = facturas;
        _uow      = uow;
    }

    public async Task Handle(AsignarCaeCommand cmd, CancellationToken ct = default)
    {
        var factura = await _facturas.GetByIdAsync(cmd.FacturaId, ct)
            ?? throw new NotFoundException($"Factura {cmd.FacturaId} not found.");

        factura.AsignarCae(cmd.Cae, cmd.Vencimiento);

        await _uow.SaveChangesAsync(ct);
    }
}
