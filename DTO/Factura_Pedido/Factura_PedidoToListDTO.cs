using DTO.Factura;
using DTO.Pedido;
using System;
using System.Collections.Generic;
using System.Text;

namespace DTO.Factura_Pedido
{
    public class Factura_PedidoToListDTO
    {
        public Guid Id_Empresa { get; set; }

        public Guid Id_Sucursal { get; set; }

        public Guid Id_Factura_Pedido { get; set; }

        public int Numero_Factura_Pedido { get; set; }

        public FacturaToListDTO Factura { get; set; }

        public PedidoToListDTO Pedido { get; set; }

    }
}
