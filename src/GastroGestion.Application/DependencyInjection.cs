using GastroGestion.Application.Facturacion.CrearFactura;
using GastroGestion.Application.Services;
using GastroGestion.Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GastroGestion.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Domain services — Application implementations (Slice C)
        services.AddScoped<IEfectivoPrecioService, EfectivoPrecioService>();
        services.AddScoped<ICalculadorFactura, CalculadorFactura>();

        // Use cases — Slice C
        services.AddScoped<CrearFacturaHandler>();

        return services;
    }
}
