using GastroGestion.Domain.Facturacion;

namespace GastroGestion.Application.Abstractions.Persistence;

public interface IFacturaRepository
{
    Task<Factura?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Factura factura, CancellationToken ct = default);
}
