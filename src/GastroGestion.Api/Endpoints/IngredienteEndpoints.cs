using GastroGestion.Api.Filters;
using GastroGestion.Application.Ingredientes.BuscarIngredientes;
using GastroGestion.Application.Ingredientes.CrearIngrediente;
using GastroGestion.Application.Ingredientes.DesactivarIngrediente;
using GastroGestion.Application.Ingredientes.EditarIngrediente;
using GastroGestion.Application.Ingredientes.GetIngredienteById;
using GastroGestion.Contracts.Ingredientes;
using GastroGestion.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GastroGestion.Api.Endpoints;

public static class IngredienteEndpoints
{
    public static WebApplication MapIngredienteEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/ingredientes").WithTags("Ingredientes").RequireAuthorization();

        // POST /ingredientes — create
        group.MapPost("/", async (
            [FromBody] CrearIngredienteRequest request,
            CrearIngredienteHandler handler,
            CancellationToken ct) =>
        {
            var id = await handler.Handle(request.ToCommand(), ct);
            return Results.Created($"/ingredientes/{id}", id);
        })
        .WithValidation<CrearIngredienteRequest>();

        // GET /ingredientes/{id} — get by id
        group.MapGet("/{id:guid}", async (
            Guid id,
            GetIngredienteByIdHandler handler,
            CancellationToken ct) =>
        {
            var ingrediente = await handler.Handle(new GetIngredienteByIdQuery(id), ct);
            return ingrediente is null
                ? Results.NotFound()
                : Results.Ok(ingrediente.ToResponse());
        });

        // GET /ingredientes?nombre=&incluirInactivos= — search (CCC-C03)
        // Default hides inactive (incluirInactivos defaults to false). ADR-CCC-3: does NOT call GetAllAsync.
        group.MapGet("/", async (
            HttpRequest request,
            BuscarIngredientesHandler handler,
            CancellationToken ct) =>
        {
            var nombre           = request.Query["nombre"].FirstOrDefault();
            var incluirInactivos = string.Equals(
                request.Query["incluirInactivos"].FirstOrDefault(), "true",
                StringComparison.OrdinalIgnoreCase);

            var list = await handler.Handle(new BuscarIngredientesQuery(nombre, incluirInactivos), ct);
            return Results.Ok(list.Select(i => i.ToResponse()).ToList());
        });

        // PUT /ingredientes/{id} — edit Nombre only (Admin only, CCC-C01)
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] EditarIngredienteRequest request,
            HttpContext http,
            EditarIngredienteHandler handler,
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

            var ingrediente = await handler.Handle(request.ToCommand(id), ct);
            return Results.Ok(ingrediente.ToResponse());
        })
        .WithValidation<EditarIngredienteRequest>();

        // DELETE /ingredientes/{id} — soft-delete (Admin only, CCC-C02)
        group.MapDelete("/{id:guid}", async (
            Guid id,
            HttpContext http,
            DesactivarIngredienteHandler handler,
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

            await handler.Handle(new DesactivarIngredienteCommand(id), ct);
            return Results.NoContent();
        });

        return app;
    }
}
