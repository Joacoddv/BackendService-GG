using GastroGestion.Application.Clientes.CrearCliente;
using GastroGestion.Application.Clientes.EditarCliente;
using GastroGestion.Domain.Clientes;

namespace GastroGestion.Contracts.Clientes;

public static class ClienteMappings
{
    public static CrearClienteCommand ToCommand(this CrearClienteRequest request)
        => new(request.Nombre, request.CondicionIVA, request.Cuit, request.Email);

    public static EditarClienteCommand ToCommand(this EditarClienteRequest request, Guid id)
        => new(id, request.Nombre, request.CondicionIVA, request.Cuit, request.Email);

    public static ClienteResponse ToResponse(this Cliente cliente)
        => new(
            cliente.Id,
            cliente.Nombre,
            cliente.CondicionIVA,
            cliente.Cuit?.Valor,
            cliente.Email?.Valor,
            cliente.Activo);
}
