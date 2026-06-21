using GastroGestion.Application.Abstractions.Persistence;
using GastroGestion.Application.Common.Exceptions;

namespace GastroGestion.Application.Ingredientes.ActualizarStockMinimo;

/// <summary>Sets an ingrediente's reorder threshold. Domain validates it is non-negative.</summary>
public sealed class ActualizarStockMinimoHandler
{
    private readonly IIngredienteRepository _ingredientes;
    private readonly IUnitOfWork            _uow;

    public ActualizarStockMinimoHandler(IIngredienteRepository ingredientes, IUnitOfWork uow)
    {
        _ingredientes = ingredientes;
        _uow          = uow;
    }

    public async Task Handle(ActualizarStockMinimoCommand cmd, CancellationToken ct = default)
    {
        var ingrediente = await _ingredientes.GetByIdAsync(cmd.IngredienteId, ct)
            ?? throw new NotFoundException($"Ingrediente '{cmd.IngredienteId}' was not found.");

        ingrediente.ActualizarStockMinimo(cmd.StockMinimo);

        await _uow.SaveChangesAsync(ct);
    }
}
