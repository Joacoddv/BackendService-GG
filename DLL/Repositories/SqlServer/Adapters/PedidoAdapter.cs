using System;
using Dominio;

namespace DLL.Repositories.SqlServer.Adapters
{
    public sealed class PedidoAdapter
    {
        private readonly static PedidoAdapter _instance = new PedidoAdapter();

        public static PedidoAdapter Current
        {
            get { return _instance; }
        }

        private PedidoAdapter()
        {
            // Implementa aquí la inicialización de tu singleton
        }

        public Pedido Adapt(object[] values)
        {
            var pedido = new Pedido
            {
                Id_Empresa = Guid.Parse(values[0].ToString()),
                Id_Sucursal = Guid.Parse(values[1].ToString()),
                Id_Pedido = Guid.Parse(values[2].ToString()),
                Numero_Pedido = Convert.ToInt32(values[3]),
                Tipo_Pedido = (ETipo_Pedido)Convert.ToInt32(values[4]),
                Fecha_Creacion = Convert.ToDateTime(values[8]),
                Fecha_Entrega = Convert.ToDateTime(values[9]),
                Fecha_Modificacion = !string.IsNullOrEmpty(values[10]?.ToString()) ? Convert.ToDateTime(values[10]) : (DateTime?)null,
                Estado = (EEstadoPedido)Convert.ToInt32(values[11]),
                Estado_Factura_Pedido = (EEstadoFacturaPedido)Convert.ToInt32(values[12]),
                Monto = Convert.ToDecimal(values[13]),
            };

            if (!string.IsNullOrEmpty(values[5]?.ToString()))
            {
                pedido.Cliente = new Cliente { Id_Cliente = Guid.Parse(values[5].ToString()) };
            }

            if (!string.IsNullOrEmpty(values[6]?.ToString()))
            {
                pedido.Direccion = new Direccion { Id_Direccion = Guid.Parse(values[6].ToString()) };
            }

            if (!string.IsNullOrEmpty(values[7]?.ToString()))
            {
                pedido.Mesa = new Mesa { Id_Mesa = Guid.Parse(values[7].ToString()) };
            }

            return pedido;
        }
    }
}
