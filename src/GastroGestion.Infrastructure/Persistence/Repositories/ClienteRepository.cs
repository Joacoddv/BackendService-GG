using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Domain.Clientes;
using Microsoft.EntityFrameworkCore;

namespace GastroGestion.Infrastructure.Persistence.Repositories;

internal sealed class ClienteRepository : IClienteRepository
{
    private readonly GastroGestionDbContext _ctx;

    public ClienteRepository(GastroGestionDbContext ctx) => _ctx = ctx;

    public Task<Cliente?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => _ctx.Clientes.FirstOrDefaultAsync(c => c.Id == id, ct);

    public async Task AddAsync(Cliente cliente, CancellationToken ct = default)
        => await _ctx.Clientes.AddAsync(cliente, ct);

    public async Task<IReadOnlyList<Cliente>> GetAllAsync(CancellationToken ct = default)
        => (await _ctx.Clientes.ToListAsync(ct)).AsReadOnly();
}
