using GastroGestion.Api.Filters;
using GastroGestion.Application.Clientes.AgregarDireccion;
using GastroGestion.Application.Clientes.BuscarClientes;
using GastroGestion.Application.Clientes.CrearCliente;
using GastroGestion.Application.Clientes.Cumpleaneros;
using GastroGestion.Application.Clientes.DesactivarCliente;
using GastroGestion.Application.Clientes.EditarCliente;
using GastroGestion.Application.Clientes.GetClienteById;
using GastroGestion.Application.Clientes.QuitarDireccion;
using GastroGestion.Contracts.Clientes;
using Microsoft.AspNetCore.Mvc;

namespace GastroGestion.Api.Endpoints;

public static class ClienteEndpoints
{
    public static WebApplication MapClienteEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/clientes").WithTags("Clientes").RequireAuthorization();

        // POST /clientes — create
        group.MapPost("/", async (
            [FromBody] CrearClienteRequest request,
            CrearClienteHandler handler,
            CancellationToken ct) =>
        {
            var id = await handler.Handle(request.ToCommand(), ct);
            return Results.Created($"/clientes/{id}", id);
        })
        .WithValidation<CrearClienteRequest>()
        .WithBitacora("Create client");

        // GET /clientes/{id} — get by id
        group.MapGet("/{id:guid}", async (
            Guid id,
            GetClienteByIdHandler handler,
            CancellationToken ct) =>
        {
            var cliente = await handler.Handle(new GetClienteByIdQuery(id), ct);
            return cliente is null
                ? Results.NotFound()
                : Results.Ok(cliente.ToResponse());
        });

        // GET /clientes?nombre=&incluirInactivos= — search (CCC-B03)
        group.MapGet("/", async (
            HttpRequest request,
            BuscarClientesHandler handler,
            CancellationToken ct) =>
        {
            var nombre            = request.Query["nombre"].FirstOrDefault();
            var incluirInactivos  = string.Equals(
                request.Query["incluirInactivos"].FirstOrDefault(), "true",
                StringComparison.OrdinalIgnoreCase);

            var list = await handler.Handle(new BuscarClientesQuery(nombre, incluirInactivos), ct);
            return Results.Ok(list.Select(c => c.ToResponse()).ToList());
        });

        // PUT /clientes/{id} — edit (Admin only, CCC-B01)
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] EditarClienteRequest request,
            EditarClienteHandler handler,
            CancellationToken ct) =>
        {
            var cliente = await handler.Handle(request.ToCommand(id), ct);
            return Results.Ok(cliente.ToResponse());
        })
        .WithValidation<EditarClienteRequest>()
        .RequireAuthorization("SoloAdministrador")
        .WithBitacora("Update client");

        // DELETE /clientes/{id} — soft-delete (Admin only, CCC-B02)
        group.MapDelete("/{id:guid}", async (
            Guid id,
            DesactivarClienteHandler handler,
            CancellationToken ct) =>
        {
            await handler.Handle(new DesactivarClienteCommand(id), ct);
            return Results.NoContent();
        })
        .RequireAuthorization("SoloAdministrador")
        .WithBitacora("Deactivate client");

        // GET /clientes/cumpleaneros?mes=
        group.MapGet("/cumpleaneros", async (
            HttpRequest request,
            GetCumpleanerosHandler handler,
            CancellationToken ct) =>
        {
            var mes = ParseMes(request.Query["mes"].FirstOrDefault());
            var lista = await handler.Handle(new GetCumpleanerosQuery(mes), ct);
            return Results.Ok(lista.Select(c => c.ToResponse()).ToList());
        });

        // POST /clientes/cumpleaneros/enviar-promo?mes=
        group.MapPost("/cumpleaneros/enviar-promo", async (
            HttpRequest request,
            EnviarPromoCumpleanosHandler handler,
            CancellationToken ct) =>
        {
            var mes = ParseMes(request.Query["mes"].FirstOrDefault());
            var result = await handler.Handle(new EnviarPromoCumpleanosCommand(mes), ct);
            return Results.Ok(result.ToResponse());
        });

        // POST /clientes/{id}/direcciones
        group.MapPost("/{id:guid}/direcciones", async (
            Guid id,
            [FromBody] AgregarDireccionRequest request,
            AgregarDireccionHandler handler,
            GetClienteByIdHandler getHandler,
            CancellationToken ct) =>
        {
            await handler.Handle(new AgregarDireccionCommand(
                id, request.Calle, request.Numero, request.Ciudad, request.Provincia,
                request.CodigoPostal, request.Piso, request.Departamento), ct);

            var cliente = await getHandler.Handle(new GetClienteByIdQuery(id), ct);
            return Results.Ok(cliente!.ToResponse());
        })
        .WithBitacora("Add client address");

        // DELETE /clientes/{id}/direcciones/{direccionId}
        group.MapDelete("/{id:guid}/direcciones/{direccionId:guid}", async (
            Guid id,
            Guid direccionId,
            QuitarDireccionHandler handler,
            GetClienteByIdHandler getHandler,
            CancellationToken ct) =>
        {
            await handler.Handle(new QuitarDireccionCommand(id, direccionId), ct);

            var cliente = await getHandler.Handle(new GetClienteByIdQuery(id), ct);
            return cliente is null ? Results.NotFound() : Results.Ok(cliente.ToResponse());
        })
        .WithBitacora("Remove client address");

        return app;
    }

    /// <summary>Parses a 1-12 month query value; falls back to the current UTC month.</summary>
    private static int ParseMes(string? raw)
        => int.TryParse(raw, out var m) && m is >= 1 and <= 12 ? m : DateTime.UtcNow.Month;
}
