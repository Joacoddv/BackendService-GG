using GastroGestion.Api.Filters;
using GastroGestion.Application.Platos.CrearPlato;
using GastroGestion.Application.Platos.DesactivarPlato;
using GastroGestion.Application.Platos.EditarPlato;
using GastroGestion.Application.Platos.GetAllPlatos;
using GastroGestion.Application.Platos.GetPlatoById;
using GastroGestion.Contracts.Platos;
using GastroGestion.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GastroGestion.Api.Endpoints;

public static class PlatoEndpoints
{
    public static WebApplication MapPlatoEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/platos").WithTags("Platos").RequireAuthorization();

        group.MapPost("/", async (
            [FromBody] CrearPlatoRequest request,
            CrearPlatoHandler handler,
            CancellationToken ct) =>
        {
            var id = await handler.Handle(request.ToCommand(), ct);
            return Results.Created($"/platos/{id}", id);
        })
        .WithValidation<CrearPlatoRequest>();

        group.MapGet("/{id:guid}", async (
            Guid id,
            GetPlatoByIdHandler handler,
            CancellationToken ct) =>
        {
            var plato = await handler.Handle(new GetPlatoByIdQuery(id), ct);
            return plato is null
                ? Results.NotFound()
                : Results.Ok(plato.ToResponse());
        });

        group.MapGet("/", async (
            GetAllPlatosHandler handler,
            CancellationToken ct) =>
        {
            var list = await handler.Handle(new GetAllPlatosQuery(), ct);
            return Results.Ok(list.Select(p => p.ToResponse()).ToList());
        });

        // PUT /platos/{id} — edit Nombre and PrecioBase (Admin only)
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] EditarPlatoRequest request,
            HttpContext http,
            EditarPlatoHandler handler,
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

            var plato = await handler.Handle(request.ToCommand(id), ct);
            return Results.Ok(plato.ToResponse());
        })
        .WithValidation<EditarPlatoRequest>();

        // DELETE /platos/{id} — soft-delete (Admin only)
        group.MapDelete("/{id:guid}", async (
            Guid id,
            HttpContext http,
            DesactivarPlatoHandler handler,
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

            await handler.Handle(new DesactivarPlatoCommand(id), ct);
            return Results.NoContent();
        });

        return app;
    }
}
