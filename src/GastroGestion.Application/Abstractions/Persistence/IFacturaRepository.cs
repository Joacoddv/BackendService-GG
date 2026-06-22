using GastroGestion.Domain.Enums;
using GastroGestion.Domain.Facturacion;

namespace GastroGestion.Application.Abstractions.Persistence;

public interface IFacturaRepository
{
    Task<Factura?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Factura factura, CancellationToken ct = default);
    Task<IReadOnlyList<Factura>> ListAsync(EstadoFactura? estado, Guid? clienteId, CancellationToken ct = default);
}
