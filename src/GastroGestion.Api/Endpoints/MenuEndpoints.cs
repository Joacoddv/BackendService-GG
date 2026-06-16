using GastroGestion.Api.Filters;
using GastroGestion.Application.Menus.CrearMenu;
using GastroGestion.Application.Menus.GetAllMenus;
using GastroGestion.Application.Menus.GetMenuById;
using GastroGestion.Contracts.Menus;
using Microsoft.AspNetCore.Mvc;

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

        return app;
    }
}
