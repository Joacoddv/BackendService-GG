using GastroGestion.Api.Filters;
using GastroGestion.Application.Facturacion.AnularFactura;
using GastroGestion.Application.Facturacion.CancelarFactura;
using GastroGestion.Application.Facturacion.CrearFactura;
using GastroGestion.Application.Facturacion.GetFacturaById;
using GastroGestion.Application.Facturacion.GetFacturas;
using GastroGestion.Application.Facturacion.RegistrarPago;
using GastroGestion.Contracts.Facturacion;
using GastroGestion.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace GastroGestion.Api.Endpoints;

public static class FacturaEndpoints
{
    public static WebApplication MapFacturaEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/facturas").WithTags("Facturas").RequireAuthorization();

        // POST /facturas — create invoice from confirmed pedidos
        group.MapPost("/", async (
            [FromBody] CrearFacturaRequest request,
            CrearFacturaHandler handler,
            CancellationToken ct) =>
        {
            var id = await handler.Handle(request.ToCommand(), ct);
            return Results.Created($"/facturas/{id}", id);
        })
        .WithValidation<CrearFacturaRequest>();

        // GET /facturas — list invoices with optional filters
        group.MapGet("/", async (
            [FromQuery] EstadoFactura? estado,
            [FromQuery] Guid? clienteId,
            GetFacturasHandler handler,
            CancellationToken ct) =>
        {
            var facturas = await handler.Handle(new GetFacturasQuery(estado, clienteId), ct);
            return Results.Ok(facturas.Select(f => f.ToResumenResponse()).ToList());
        });

        // POST /facturas/{id}/pagos — register a payment
        group.MapPost("/{id:guid}/pagos", async (
            Guid id,
            [FromBody] RegistrarPagoRequest request,
            RegistrarPagoHandler handler,
            CancellationToken ct) =>
        {
            await handler.Handle(request.ToCommand(id), ct);
            return Results.NoContent();
        })
        .WithValidation<RegistrarPagoRequest>();

        // POST /facturas/{id}/cancelar — cancel an invoice
        group.MapPost("/{id:guid}/cancelar", async (
            Guid id,
            CancelarFacturaHandler handler,
            CancellationToken ct) =>
        {
            await handler.Handle(new CancelarFacturaCommand(id), ct);
            return Results.NoContent();
        });

        // POST /facturas/{id}/anular — annul a paid invoice via credit note
        group.MapPost("/{id:guid}/anular", async (
            Guid id,
            [FromBody] AnularFacturaRequest request,
            AnularFacturaHandler handler,
            CancellationToken ct) =>
        {
            await handler.Handle(request.ToCommand(id), ct);
            return Results.NoContent();
        })
        .WithValidation<AnularFacturaRequest>();

        // GET /facturas/{id} — get invoice by id
        group.MapGet("/{id:guid}", async (
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
