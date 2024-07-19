using System;
using System.Collections.Generic;
using System.Text;

namespace DTO
{

    public enum EBusquedaCliente
    {
        Id = 1,
        Numero_Exacto = 2,
        Numero_Cliente = 3,
        Nombre_Cliente = 4,
        Apellido_Cliente = 5,
        Nro_Doc_Cliente = 6,
        Estado_Cliente = 7
    }
    public enum EBusquedaIngrediente { Id = 1, Numero_Ingrediente = 2, Nombre_Ingrediente = 3, Nombre_exacto_Ingrediente = 4, Descripcion_Ingrediente = 5, Medida_Ingrediente = 6 }

    public enum EBusquedaDireccion { Id = 1, Numero_Direccion = 2, Numero_Cliente = 3 }

    public enum EBusquedaPlato { Id = 1, Numero_Plato = 2, Nombre_Plato = 3, Descipcion_Plato = 4, Plato_x_Ingrediente = 5, PLato_x_Nombre_Plato_Exacto = 6 }

    public enum EBusquedaPlato_Ingrediente { Id = 1, Ingredientes_x_Plato = 2, PlatoxIngrediente = 3, }

    public enum EBusquedaMenu { Id = 1, MenuxNumeroMenu = 2, MenuxFecha = 3, MenuxPlato = 4, BuscarPreioMenuxPlatoyFecha = 5 }

    public enum EBusquedaPedido { Id = 1, BuscarPedidosxNumeroPedido = 2, BuscarPedidoxNumeroPedidoExacto = 3, BuscarPedidosDisponiblesparaFacturarxxIdCliente = 4, BuscarPedidosxCliente = 5, BuscarPedidosxDireccion = 6, BuscarPedidosxMesa = 7, BuscarPedidosxEstadoPedido = 8, BuscarPedidosxEstadoFacturaPedido = 9, BuscarPedidosxFechaPedido = 10, BuscarPedidosxFechaEntregaPedido = 11, HidratarPedido = 12, ValidarEstadosPosibles = 13 }

    public enum EBusquedaPlato_Pedido { Id = 1, GetOnePedido = 2, BuscarPlatoPedidoxPedido = 3, CalcularMontoPedido = 4 }

    public enum EBusquedaMesa { Id = 1, NumeroMesa = 2, NumeroExactoMesa = 3, Capacidad =4 }

    public enum EBusquedaPlatoPrecio { Id = 1, BuscarPrecioPorPlatoYFecha = 2, BuscarPlatoPrecioXPlato = 3 }

    public enum EBusquedaFactura { Id = 1, BuscarFacturaxNumeroFacturaExacto = 2, BuscarFacturaxFechaFacturaExacto = 3, BuscarFacturaxCliente = 4, BuscarFacturaxEstadoFactura = 5, BuscarFacturaxNumeroPedido = 6, ValidarEstadosPosibles = 7 }

    public enum EBusquedaFactura_Pedido { Id = 1, ValidarPedidoenFactura = 2, BuscarFacturaxNumeroPedido = 3 }

    

}
