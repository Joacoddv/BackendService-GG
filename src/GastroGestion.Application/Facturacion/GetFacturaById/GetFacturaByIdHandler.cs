using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Facturacion;

namespace GastroGestion.Application.Facturacion.GetFacturaById;

public sealed class GetFacturaByIdHandler
{
    private readonly IFacturaRepository _facturas;

    public GetFacturaByIdHandler(IFacturaRepository facturas) => _facturas = facturas;

    public async Task<Factura?> Handle(GetFacturaByIdQuery query, CancellationToken ct = default)
        => await _facturas.GetByIdAsync(query.Id, ct);
}
