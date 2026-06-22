using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;

namespace GastroGestion.Application.Facturacion.CancelarFactura;

public sealed class CancelarFacturaHandler
{
    private readonly IFacturaRepository _facturas;
    private readonly IUnitOfWork        _uow;

    public CancelarFacturaHandler(IFacturaRepository facturas, IUnitOfWork uow)
    {
        _facturas = facturas;
        _uow      = uow;
    }

    public async Task Handle(CancelarFacturaCommand cmd, CancellationToken ct = default)
    {
        var factura = await _facturas.GetByIdAsync(cmd.FacturaId, ct)
            ?? throw new NotFoundException($"Factura {cmd.FacturaId} not found.");

        factura.Cancelar();

        await _uow.SaveChangesAsync(ct);
    }
}
