using GastroGestion.Api.Filters;
using GastroGestion.Application.Facturacion.CrearFactura;
using GastroGestion.Application.Facturacion.GetFacturaById;
using GastroGestion.Application.Facturacion.RegistrarPago;
using GastroGestion.Contracts.Facturacion;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GastroGestion.Api.Endpoints;

public static class FacturaEndpoints
{
    public static WebApplication MapFacturaEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/facturas").WithTags("Facturas");

        // POST /facturas — create invoice from confirmed pedidos
        group.MapPost("/", [AllowAnonymous] async (
            [FromBody] CrearFacturaRequest request,
            CrearFacturaHandler handler,
            CancellationToken ct) =>
        {
            var id = await handler.Handle(request.ToCommand(), ct);
            return Results.Created($"/facturas/{id}", id);
        })
        .WithValidation<CrearFacturaRequest>();

        // POST /facturas/{id}/pagos — register a payment
        group.MapPost("/{id:guid}/pagos", [AllowAnonymous] async (
            Guid id,
            [FromBody] RegistrarPagoRequest request,
            RegistrarPagoHandler handler,
            CancellationToken ct) =>
        {
            await handler.Handle(request.ToCommand(id), ct);
            return Results.NoContent();
        })
        .WithValidation<RegistrarPagoRequest>();

        // GET /facturas/{id} — get invoice by id
        group.MapGet("/{id:guid}", [AllowAnonymous] async (
            Guid id,
            GetFacturaByIdHandler handler,
            CancellationToken ct) =>
        {
            var factura = await handler.Handle(new GetFacturaByIdQuery(id), ct);
            return factura is null
                ? Results.NotFound()
                : Results.Ok(factura.ToResponse());
        });

        return app;
    }
}
