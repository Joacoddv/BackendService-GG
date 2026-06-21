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
using GastroGestion.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

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
        .WithValidation<CrearClienteRequest>();

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
        // Default hides inactive (incluirInactivos defaults to false). ADR-CCC-3: does NOT call GetAllAsync.
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
            HttpContext http,
            EditarClienteHandler handler,
            CancellationToken ct) =>
        {
            var rolClaim = http.User.FindFirst(ClaimTypes.Role)?.Value;
            if (rolClaim is null || !Enum.TryParse<RolUsuario>(rolClaim, out var rol))
                return Results.Problem(
                    title: "Invalid or missing role claim.",
                    statusCode: StatusCodes.Status403Forbidden);

            if (rol is not RolUsuario.Administrador)
                return Results.Problem(
                    title: "Access denied. Required role: Administrador.",
                    statusCode: StatusCodes.Status403Forbidden);

            var cliente = await handler.Handle(request.ToCommand(id), ct);
            return Results.Ok(cliente.ToResponse());
        })
        .WithValidation<EditarClienteRequest>();

        // DELETE /clientes/{id} — soft-delete (Admin only, CCC-B02)
        group.MapDelete("/{id:guid}", async (
            Guid id,
            HttpContext http,
            DesactivarClienteHandler handler,
            CancellationToken ct) =>
        {
            var rolClaim = http.User.FindFirst(ClaimTypes.Role)?.Value;
            if (rolClaim is null || !Enum.TryParse<RolUsuario>(rolClaim, out var rol))
                return Results.Problem(
                    title: "Invalid or missing role claim.",
                    statusCode: StatusCodes.Status403Forbidden);

            if (rol is not RolUsuario.Administrador)
                return Results.Problem(
                    title: "Access denied. Required role: Administrador.",
                    statusCode: StatusCodes.Status403Forbidden);

            await handler.Handle(new DesactivarClienteCommand(id), ct);
            return Results.NoContent();
        });

        // GET /clientes/cumpleaneros?mes= — clientes whose birthday is in the month (default: current)
        group.MapGet("/cumpleaneros", async (
            HttpRequest request,
            GetCumpleanerosHandler handler,
            CancellationToken ct) =>
        {
            var mes = ParseMes(request.Query["mes"].FirstOrDefault());
            var lista = await handler.Handle(new GetCumpleanerosQuery(mes), ct);
            return Results.Ok(lista.Select(c => c.ToResponse()).ToList());
        });

        // POST /clientes/cumpleaneros/enviar-promo?mes= — email a birthday promo to the month's clientes
        group.MapPost("/cumpleaneros/enviar-promo", async (
            HttpRequest request,
            EnviarPromoCumpleanosHandler handler,
            CancellationToken ct) =>
        {
            var mes = ParseMes(request.Query["mes"].FirstOrDefault());
            var result = await handler.Handle(new EnviarPromoCumpleanosCommand(mes), ct);
            return Results.Ok(result.ToResponse());
        });

        // POST /clientes/{id}/direcciones — add an address; returns the updated cliente
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
        });

        // DELETE /clientes/{id}/direcciones/{direccionId} — remove an address; returns the updated cliente
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
        });

        return app;
    }

    /// <summary>Parses a 1-12 month query value; falls back to the current UTC month.</summary>
    private static int ParseMes(string? raw)
        => int.TryParse(raw, out var m) && m is >= 1 and <= 12 ? m : DateTime.UtcNow.Month;
}
