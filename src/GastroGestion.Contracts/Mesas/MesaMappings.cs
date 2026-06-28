using GastroGestion.Application.Mesas.CrearMesa;
using GastroGestion.Application.Mesas.EditarMesa;
using GastroGestion.Application.Mesas.UbicarMesa;
using GastroGestion.Domain.Mesas;

namespace GastroGestion.Contracts.Mesas;

public static class MesaMappings
{
    public static CrearMesaCommand ToCommand(this CrearMesaRequest request)
        => new(request.Numero, request.Capacidad);

    public static EditarMesaCommand ToCommand(this EditarMesaRequest request, Guid id)
        => new(id, request.Numero, request.Capacidad);

    public static UbicarMesaCommand ToCommand(this UbicarMesaRequest request, Guid id)
        => new(id, request.X, request.Y);

    public static MesaResponse ToResponse(this Mesa mesa)
        => new(mesa.Id, mesa.Numero, mesa.Capacidad, mesa.Estado, mesa.Activa, mesa.PedidoActivoId, mesa.PosicionX, mesa.PosicionY);
}
