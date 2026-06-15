using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;
using GastroGestion.Domain.ValueObjects;

namespace GastroGestion.Application.Facturacion.RegistrarPago;

public sealed class RegistrarPagoHandler
{
    private readonly IFacturaRepository _facturas;
    private readonly IUnitOfWork        _uow;

    public RegistrarPagoHandler(IFacturaRepository facturas, IUnitOfWork uow)
    {
        _facturas = facturas;
        _uow      = uow;
    }

    public async Task Handle(RegistrarPagoCommand cmd, CancellationToken ct = default)
    {
        var factura = await _facturas.GetByIdAsync(cmd.FacturaId, ct)
            ?? throw new NotFoundException($"Factura {cmd.FacturaId} not found.");

        factura.RegistrarPago(new Dinero(cmd.Monto), cmd.MetodoPago, DateTime.UtcNow);

        await _uow.SaveChangesAsync(ct);
    }
}
