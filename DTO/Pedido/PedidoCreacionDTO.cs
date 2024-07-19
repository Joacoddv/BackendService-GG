using Dominio;
using System;
using System.Collections.Generic;
using System.Text;
using DTO.Cliente;
using DTO.Direcciones;
using DTO.Mesa;

namespace DTO.Pedido
{
    public class PedidoCreacionDTO
    {

        public Guid Id_Empresa { get; set; }

        public Guid Id_Sucursal { get; set; }

        public Guid Id_Pedido { get; set; } = Guid.NewGuid();

        public ETipo_Pedido Tipo_Pedido { get; set; }

        public ClienteToListDTO Cliente { get; set; }

        public DireccionToListDTO Direccion { get; set; }

        public MesaToListDTO Mesa { get; set; }

        public DateTime Fecha_Creacion { get; set; } = DateTime.Now;

        public DateTime Fecha_Entrega { get; set; }

        public DateTime? Fecha_Modificacion { get; set; } = DateTime.Now;

        public EEstadoPedido Estado { get; set; } = EEstadoPedido.Creado;

        public decimal Monto { get; set; }

        public EEstadoFacturaPedido Estado_Factura_Pedido { get; set; } = EEstadoFacturaPedido.No_Facturado;

        public PedidoCreacionDTO()
        {
            Id_Pedido = Guid.NewGuid();
            Fecha_Creacion = DateTime.Now;
            Estado = EEstadoPedido.Creado;
            Estado_Factura_Pedido = EEstadoFacturaPedido.No_Facturado;
        }
    }
}
