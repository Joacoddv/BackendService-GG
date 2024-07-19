using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio
{
    public class Factura_Pedido
    {
        public Guid Id_Empresa { get; set; }

        public Guid Id_Sucursal { get; set; }

        public Guid Id_Factura_Pedido { get; set; }

        public int Numero_Factura_Pedido { get; set; }

        public Factura Factura { get; set; }

        public Pedido Pedido { get; set; }

        public Factura_Pedido(Guid id_empresa, Guid id_sucursal, Guid id_factura_pedido, int numero_factura_pedido, Factura factura, Pedido pedido)
        {
            Id_Empresa = id_empresa;
            Id_Sucursal = id_sucursal;
            Id_Factura_Pedido = id_factura_pedido;
            Numero_Factura_Pedido = numero_factura_pedido;
            Factura = factura;
            Pedido = pedido;
        }

        public Factura_Pedido()
        { }
    }
}
