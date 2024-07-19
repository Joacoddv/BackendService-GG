using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio
{
    public class Pedido
    {
        public Guid Id_Empresa { get; set; }

        public Guid Id_Sucursal { get; set; }

        public Guid Id_Pedido { get; set; }

        public int Numero_Pedido { get; set; }

        public ETipo_Pedido Tipo_Pedido { get; set; }

        public Cliente Cliente { get; set; }

        public Direccion Direccion { get; set; }

        public Mesa Mesa { get; set; }

        public DateTime Fecha_Creacion { get; set; }

        public DateTime Fecha_Entrega { get; set; }

        public DateTime? Fecha_Modificacion { get; set; }

        public EEstadoPedido Estado { get; set; }

        public decimal Monto { get; set; }

        public EEstadoFacturaPedido Estado_Factura_Pedido { get; set; }

        public Pedido(Guid id_empresa, Guid id_sucursal, Guid id_pedido, int numero_pedido,ETipo_Pedido tipo_pedido,Cliente cliente, Direccion direccion,Mesa mesa, DateTime fecha_creacion, DateTime fecha_entrega, DateTime fecha_modificacion, EEstadoPedido estado, decimal monto,EEstadoFacturaPedido estado_factura_pedido)
        {
            Id_Empresa = id_empresa;
            Id_Sucursal = id_sucursal;
            Id_Pedido = id_pedido;
            Numero_Pedido = numero_pedido;
            Tipo_Pedido = tipo_pedido;
            Cliente = cliente;
            Direccion = direccion;
            Mesa = mesa;
            Fecha_Creacion = fecha_creacion;
            Fecha_Entrega = fecha_entrega;
            Fecha_Modificacion = fecha_modificacion;
            Estado = estado;
            Monto = monto;
            Estado_Factura_Pedido = estado_factura_pedido;
        }

        public Pedido()
        {

        }
    }
}
