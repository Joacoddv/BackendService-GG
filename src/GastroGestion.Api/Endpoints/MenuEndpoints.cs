using GastroGestion.Api.Filters;
using GastroGestion.Application.Menus.CrearMenu;
using GastroGestion.Application.Menus.DesactivarMenu;
using GastroGestion.Application.Menus.EditarMenu;
using GastroGestion.Application.Menus.GetAllMenus;
using GastroGestion.Application.Menus.GetMenuById;
using GastroGestion.Contracts.Menus;
using GastroGestion.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace GastroGestion.Api.Endpoints;

public static class MenuEndpoints
{
    public static WebApplication MapMenuEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/menus").WithTags("Menus").RequireAuthorization();

        group.MapPost("/", async (
            [FromBody] CrearMenuRequest request,
            CrearMenuHandler handler,
            CancellationToken ct) =>
        {
            var id = await handler.Handle(request.ToCommand(), ct);
            return Results.Created($"/menus/{id}", id);
        })
        .WithValidation<CrearMenuRequest>();

        group.MapGet("/{id:guid}", async (
            Guid id,
            GetMenuByIdHandler handler,
            CancellationToken ct) =>
        {
            var menu = await handler.Handle(new GetMenuByIdQuery(id), ct);
            return menu is null
                ? Results.NotFound()
                : Results.Ok(menu.ToResponse());
        });

        group.MapGet("/", async (
            GetAllMenusHandler handler,
            CancellationToken ct) =>
        {
            var list = await handler.Handle(new GetAllMenusQuery(), ct);
            return Results.Ok(list.Select(m => m.ToResponse()).ToList());
        });

        // PUT /menus/{id} — edit Nombre and FechaVigencia (Admin only)
        group.MapPut("/{id:guid}", async (
            Guid id,
            [FromBody] EditarMenuRequest request,
            HttpContext http,
            EditarMenuHandler handler,
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

            var menu = await handler.Handle(request.ToCommand(id), ct);
            return Results.Ok(menu.ToResponse());
        })
        .WithValidation<EditarMenuRequest>();

        // DELETE /menus/{id} — soft-delete (Admin only)
        group.MapDelete("/{id:guid}", async (
            Guid id,
            HttpContext http,
            DesactivarMenuHandler handler,
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

            await handler.Handle(new DesactivarMenuCommand(id), ct);
            return Results.NoContent();
        });

        return app;
    }
}
