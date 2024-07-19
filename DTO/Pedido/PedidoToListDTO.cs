using Dominio;
using DTO.Cliente;
using DTO.Direcciones;
using DTO.Mesa;
using System;
using System.Collections.Generic;
using System.Text;

namespace DTO.Pedido
{
    public class PedidoToListDTO
    {
        public Guid Id_Empresa { get; set; }

        public Guid Id_Sucursal { get; set; }

        public Guid Id_Pedido { get; set; }

        public ETipo_Pedido Tipo_Pedido { get; set; }

        public ClienteToListDTO Cliente { get; set; }

        public DireccionToListDTO Direccion { get; set; }

        public MesaToListDTO Mesa { get; set; }

        public DateTime Fecha_Creacion { get; set; }

        public DateTime Fecha_Entrega { get; set; }

        public DateTime? Fecha_Modificacion { get; set; }

        public EEstadoPedido Estado { get; set; }

        public decimal Monto { get; set; }

        public EEstadoFacturaPedido Estado_Factura_Pedido { get; set; }
    }
}
