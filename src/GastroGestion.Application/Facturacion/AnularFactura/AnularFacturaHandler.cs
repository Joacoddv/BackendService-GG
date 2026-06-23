using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;

namespace GastroGestion.Application.Facturacion.AnularFactura;

public sealed class AnularFacturaHandler
{
    private readonly IFacturaRepository _facturas;
    private readonly IUnitOfWork        _uow;

    public AnularFacturaHandler(IFacturaRepository facturas, IUnitOfWork uow)
    {
        _facturas = facturas;
        _uow      = uow;
    }

    public async Task Handle(AnularFacturaCommand cmd, CancellationToken ct = default)
    {
        var factura = await _facturas.GetByIdAsync(cmd.FacturaId, ct)
            ?? throw new NotFoundException($"Factura {cmd.FacturaId} not found.");

        factura.Anular(cmd.Motivo, DateTime.UtcNow);

        await _uow.SaveChangesAsync(ct);
    }
}
