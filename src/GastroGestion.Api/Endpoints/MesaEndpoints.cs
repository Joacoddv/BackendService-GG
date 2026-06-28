using GastroGestion.Api.Filters;
using GastroGestion.Application.Mesas.CrearMesa;
using GastroGestion.Application.Mesas.DesactivarMesa;
using GastroGestion.Application.Mesas.EditarMesa;
using GastroGestion.Application.Mesas.GetAllMesas;
using GastroGestion.Application.Mesas.GetMesaById;
using GastroGestion.Application.Mesas.UbicarMesa;
using GastroGestion.Contracts.Mesas;
using GastroGestion.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GastroGestion.Api.Endpoints;

public static class MesaEndpoints
{
    public static WebApplication MapMesaEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/mesas").WithTags("Mesas").RequireAuthorization();

        group.MapPost("/", async (
            [FromBody] CrearMesaRequest request,
            CrearMesaHandler handler,
            CancellationToken ct) =>
        {
            var id = await handler.Handle(request.ToCommand(), ct);
            return Results.Created($"/mesas/{id}", id);
        })
        .WithValidation<CrearMesaRequest>();

        group.MapGet("/{id:guid}", async (
            Guid id,
            GetMesaByIdHandler handler,
            CancellationToken ct) =>
        {
            var mesa = await handler.Handle(new GetMesaByIdQuery(id), ct);
            return mesa is null
                ? Results.NotFound()
                : Results.Ok(mesa.ToResponse());
        });

        group.MapGet("/", async (
            GetAllMesasHandler handler,
            CancellationToken ct) =>
        {
            var list = await handler.Handle(new GetAllMesasQuery(), ct);
            return Results.Ok(list.Select(m => m.ToResponse()).ToList());
        });

        // PUT /mesas/{id} — edit Numero and Capacidad (Admin only)
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] EditarMesaRequest request,
            HttpContext http,
            EditarMesaHandler handler,
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

            var mesa = await handler.Handle(request.ToCommand(id), ct);
            return Results.Ok(mesa.ToResponse());
        })
        .WithValidation<EditarMesaRequest>();

        // PUT /mesas/{id}/posicion — set floor-plan coordinates (Admin only)
        group.MapPut("/{id:guid}/posicion", async (
            Guid id,
            [FromBody] UbicarMesaRequest request,
            HttpContext http,
            UbicarMesaHandler handler,
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

            var mesa = await handler.Handle(request.ToCommand(id), ct);
            return Results.Ok(mesa.ToResponse());
        })
        .WithValidation<UbicarMesaRequest>();

        // DELETE /mesas/{id} — soft-delete (Admin only)
        group.MapDelete("/{id:guid}", async (
            Guid id,
            HttpContext http,
            DesactivarMesaHandler handler,
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

            await handler.Handle(new DesactivarMesaCommand(id), ct);
            return Results.NoContent();
        });

        return app;
    }
}
