using GastroGestion.Domain.Ingredientes;

namespace GastroGestion.Application.Abstractions.Persistence;

public interface IIngredienteRepository
{
    Task<Ingrediente?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(Ingrediente ingrediente, CancellationToken ct = default);
}
