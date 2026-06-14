using GastroGestion.Application.Clientes.CrearCliente;
using GastroGestion.Application.Clientes.GetAllClientes;
using GastroGestion.Application.Clientes.GetClienteById;
using GastroGestion.Application.Facturacion.CrearFactura;
using GastroGestion.Application.Ingredientes.CrearIngrediente;
using GastroGestion.Application.Ingredientes.GetAllIngredientes;
using GastroGestion.Application.Ingredientes.GetIngredienteById;
using GastroGestion.Application.Menus.CrearMenu;
using GastroGestion.Application.Menus.GetAllMenus;
using GastroGestion.Application.Menus.GetMenuById;
using GastroGestion.Application.Mesas.CrearMesa;
using GastroGestion.Application.Mesas.GetAllMesas;
using GastroGestion.Application.Mesas.GetMesaById;
using GastroGestion.Application.Platos.CrearPlato;
using GastroGestion.Application.Platos.GetAllPlatos;
using GastroGestion.Application.Platos.GetPlatoById;
using GastroGestion.Application.Services;
using GastroGestion.Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GastroGestion.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Domain services — Application implementations
        services.AddScoped<IEfectivoPrecioService, EfectivoPrecioService>();
        services.AddScoped<ICalculadorFactura, CalculadorFactura>();

        // Use cases — Slice C (Facturacion, existing)
        services.AddScoped<CrearFacturaHandler>();

        // Use cases — Slice B (Catalogue)
        services.AddScoped<CrearClienteHandler>();
        services.AddScoped<GetClienteByIdHandler>();
        services.AddScoped<GetAllClientesHandler>();

        services.AddScoped<CrearIngredienteHandler>();
        services.AddScoped<GetIngredienteByIdHandler>();
        services.AddScoped<GetAllIngredientesHandler>();

        services.AddScoped<CrearPlatoHandler>();
        services.AddScoped<GetPlatoByIdHandler>();
        services.AddScoped<GetAllPlatosHandler>();

        services.AddScoped<CrearMenuHandler>();
        services.AddScoped<GetMenuByIdHandler>();
        services.AddScoped<GetAllMenusHandler>();

        services.AddScoped<CrearMesaHandler>();
        services.AddScoped<GetMesaByIdHandler>();
        services.AddScoped<GetAllMesasHandler>();

        return services;
    }
}
