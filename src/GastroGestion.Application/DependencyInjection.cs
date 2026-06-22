using GastroGestion.Application.Auth.CerrarSesion;
using GastroGestion.Application.Auth.CerrarSesionGlobal;
using GastroGestion.Application.Auth.Login;
using GastroGestion.Application.Auth.RefrescarToken;
using GastroGestion.Application.Clientes.AgregarDireccion;
using GastroGestion.Application.Clientes.BuscarClientes;
using GastroGestion.Application.Clientes.Cumpleaneros;
using GastroGestion.Application.Clientes.QuitarDireccion;
using GastroGestion.Application.Dashboard.GetDashboard;
using GastroGestion.Application.Clientes.CrearCliente;
using GastroGestion.Application.Clientes.DesactivarCliente;
using GastroGestion.Application.Clientes.EditarCliente;
using GastroGestion.Application.Usuarios.BuscarUsuarios;
using GastroGestion.Application.Usuarios.CrearUsuario;
using GastroGestion.Application.Usuarios.DesactivarUsuario;
using GastroGestion.Application.Usuarios.EditarUsuario;
using GastroGestion.Application.Usuarios.GetCocineros;
using GastroGestion.Application.Usuarios.GetUsuarioById;
using GastroGestion.Application.Clientes.GetAllClientes;
using GastroGestion.Application.Clientes.GetClienteById;
using GastroGestion.Application.Facturacion.CancelarFactura;
using GastroGestion.Application.Facturacion.CrearFactura;
using GastroGestion.Application.Facturacion.GetFacturaById;
using GastroGestion.Application.Facturacion.GetFacturas;
using GastroGestion.Application.Facturacion.RegistrarPago;
using GastroGestion.Application.Ingredientes.ActualizarStockMinimo;
using GastroGestion.Application.Ingredientes.BuscarIngredientes;
using GastroGestion.Application.Ingredientes.CrearIngrediente;
using GastroGestion.Application.Ingredientes.DesactivarIngrediente;
using GastroGestion.Application.Ingredientes.EditarIngrediente;
using GastroGestion.Application.Ingredientes.GetAllIngredientes;
using GastroGestion.Application.Ingredientes.GetIngredienteById;
using GastroGestion.Application.Menus.CrearMenu;
using GastroGestion.Application.Menus.GetAllMenus;
using GastroGestion.Application.Menus.GetMenuById;
using GastroGestion.Application.Mesas.CrearMesa;
using GastroGestion.Application.Mesas.GetAllMesas;
using GastroGestion.Application.Mesas.GetMesaById;
using GastroGestion.Application.Pedidos.ActualizarLinea;
using GastroGestion.Application.Pedidos.AsignarCocinero;
using GastroGestion.Application.Pedidos.AgregarLinea;
using GastroGestion.Application.Pedidos.BuscarPedidos;
using GastroGestion.Application.Pedidos.GenerarOrdenTrabajoLinea;
using GastroGestion.Application.Pedidos.QuitarLinea;
using GastroGestion.Application.Pedidos.ConfirmarPrecioLinea;
using GastroGestion.Application.Pedidos.CrearPedido;
using GastroGestion.Application.Pedidos.GenerarOrdenesTrabajo;
using GastroGestion.Application.Pedidos.GetOrdenesByEstado;
using GastroGestion.Application.Pedidos.GetPedidoById;
using GastroGestion.Application.Pedidos.MarcarOrdenTrabajoLista;
using GastroGestion.Application.Pedidos.TransicionarEstadoPedido;
using GastroGestion.Application.Platos.CrearPlato;
using GastroGestion.Application.Proveedores.BuscarProveedores;
using GastroGestion.Application.Proveedores.CrearProveedor;
using GastroGestion.Application.Proveedores.DesactivarProveedor;
using GastroGestion.Application.Proveedores.EditarProveedor;
using GastroGestion.Application.Proveedores.GetProveedorById;
using GastroGestion.Application.Platos.GetAllPlatos;
using GastroGestion.Application.Platos.GetPlatoById;
using GastroGestion.Application.Services;
using GastroGestion.Application.Abstractions.Events;
using GastroGestion.Application.Stock.EventHandlers;
using GastroGestion.Application.Stock.GetBalanceStock;
using GastroGestion.Application.Stock.GetBalancesStock;
using GastroGestion.Application.Stock.GetMovimientosStock;
using GastroGestion.Application.Stock.RegistrarMovimientoStock;
using GastroGestion.Domain.Pedidos.Events;
using GastroGestion.Domain.Services;
using Microsoft.Extensions.DependencyInjection;

namespace GastroGestion.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Use cases — Auth (Phase 5)
        services.AddScoped<LoginHandler>();
        services.AddScoped<RefrescarTokenHandler>();
        services.AddScoped<CerrarSesionHandler>();
        services.AddScoped<CerrarSesionGlobalHandler>();

        // Domain services — Application implementations
        services.AddScoped<IEfectivoPrecioService, EfectivoPrecioService>();
        services.AddScoped<ICalculadorFactura, CalculadorFactura>();

        // Use cases — Slice A: Facturacion (existing from Phase 3)
        services.AddScoped<CrearFacturaHandler>();

        // Use cases — Slice B (Catalogue)
        services.AddScoped<CrearClienteHandler>();
        services.AddScoped<GetClienteByIdHandler>();
        services.AddScoped<GetAllClientesHandler>();
        services.AddScoped<EditarClienteHandler>();
        services.AddScoped<DesactivarClienteHandler>();
        services.AddScoped<BuscarClientesHandler>();
        services.AddScoped<AgregarDireccionHandler>();
        services.AddScoped<QuitarDireccionHandler>();
        services.AddScoped<GetCumpleanerosHandler>();
        services.AddScoped<EnviarPromoCumpleanosHandler>();

        services.AddScoped<CrearIngredienteHandler>();
        services.AddScoped<GetIngredienteByIdHandler>();
        services.AddScoped<GetAllIngredientesHandler>();
        services.AddScoped<EditarIngredienteHandler>();
        services.AddScoped<DesactivarIngredienteHandler>();
        services.AddScoped<ActualizarStockMinimoHandler>();

        // Use cases — Proveedores
        services.AddScoped<CrearProveedorHandler>();
        services.AddScoped<EditarProveedorHandler>();
        services.AddScoped<DesactivarProveedorHandler>();
        services.AddScoped<BuscarProveedoresHandler>();
        services.AddScoped<GetProveedorByIdHandler>();

        // Use case — Dashboard
        services.AddScoped<GetDashboardHandler>();
        services.AddScoped<BuscarIngredientesHandler>();

        services.AddScoped<CrearPlatoHandler>();
        services.AddScoped<GetPlatoByIdHandler>();
        services.AddScoped<GetAllPlatosHandler>();

        services.AddScoped<CrearMenuHandler>();
        services.AddScoped<GetMenuByIdHandler>();
        services.AddScoped<GetAllMenusHandler>();

        services.AddScoped<CrearMesaHandler>();
        services.AddScoped<GetMesaByIdHandler>();
        services.AddScoped<GetAllMesasHandler>();

        // Use cases — Slice C (Transactional: Pedidos)
        services.AddScoped<CrearPedidoHandler>();
        services.AddScoped<AgregarLineaHandler>();
        services.AddScoped<ActualizarLineaHandler>();
        services.AddScoped<QuitarLineaHandler>();
        services.AddScoped<GenerarOrdenTrabajoLineaHandler>();
        services.AddScoped<ConfirmarPrecioLineaHandler>();
        services.AddScoped<TransicionarEstadoPedidoHandler>();
        services.AddScoped<GetPedidoByIdHandler>();
        services.AddScoped<BuscarPedidosHandler>();

        // Use cases — Slice C (Fiscal: Facturacion)
        services.AddScoped<RegistrarPagoHandler>();
        services.AddScoped<GetFacturaByIdHandler>();
        services.AddScoped<GetFacturasHandler>();
        services.AddScoped<CancelarFacturaHandler>();

        // Use cases — Slice C (Stock)
        services.AddScoped<RegistrarMovimientoStockHandler>();
        services.AddScoped<GetBalanceStockHandler>();
        services.AddScoped<GetBalancesStockHandler>();
        services.AddScoped<GetMovimientosStockHandler>();

        // Domain-event handlers — OT lifecycle drives the stock ledger (reserve → consume → release)
        services.AddScoped<IDomainEventHandler<OrdenTrabajoCreada>, ReservarStockOnOrdenTrabajoCreada>();
        services.AddScoped<IDomainEventHandler<OrdenTrabajoIniciada>, ConsumirStockOnOrdenTrabajoIniciada>();
        services.AddScoped<IDomainEventHandler<StockDebeRestaurarse>, RestaurarStockOnStockDebeRestaurarse>();

        // Use cases — Slice D: Kitchen (Phase 6)
        services.AddScoped<GenerarOrdenesTrabajoHandler>();
        services.AddScoped<AsignarCocineroHandler>();
        services.AddScoped<MarcarOrdenTrabajoListaHandler>();
        services.AddScoped<GetOrdenesByEstadoHandler>();

        // Use cases — Slice E: Cocineros listing (CCC-A01)
        services.AddScoped<GetCocinerosHandler>();

        // Use cases — Usuarios management CRUD (Phase-5 debt)
        services.AddScoped<CrearUsuarioHandler>();
        services.AddScoped<GetUsuarioByIdHandler>();
        services.AddScoped<BuscarUsuariosHandler>();
        services.AddScoped<EditarUsuarioHandler>();
        services.AddScoped<DesactivarUsuarioHandler>();

        return services;
    }
}
