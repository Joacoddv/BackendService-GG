using GastroGestion.Application.Mesas.CrearMesa;
using GastroGestion.Domain.Mesas;

namespace GastroGestion.Contracts.Mesas;

public static class MesaMappings
{
    public static CrearMesaCommand ToCommand(this CrearMesaRequest request)
        => new(request.Numero, request.Capacidad);

    public static MesaResponse ToResponse(this Mesa mesa)
        => new(mesa.Id, mesa.Numero, mesa.Capacidad, mesa.Estado, mesa.Activa, mesa.PedidoActivoId);
}
