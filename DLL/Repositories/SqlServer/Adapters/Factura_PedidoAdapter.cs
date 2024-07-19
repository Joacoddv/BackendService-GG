using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dominio;

namespace DLL.Repositories.SqlServer.Adapters
{
    public sealed class Factura_PedidoAdapter
    {
        private readonly static Factura_PedidoAdapter _instance = new Factura_PedidoAdapter();

        public static Factura_PedidoAdapter Current
        {
            get
            {
                return _instance;
            }
        }

        private Factura_PedidoAdapter()
        {
            //Implent here the initialization of your singleton
        }

        public Factura_Pedido Adapt(object[] values)
        {
            return new Factura_Pedido()
            {
                Id_Empresa = Guid.Parse(values[0].ToString()),
                Id_Sucursal = Guid.Parse(values[1].ToString()),
                Id_Factura_Pedido = Guid.Parse(values[2].ToString()),
                Numero_Factura_Pedido = Convert.ToInt32(values[3]),
                Factura = new Factura { Numero_Factura = Convert.ToInt32(values[4]) },
                Pedido = new Pedido { Numero_Pedido = Convert.ToInt32(values[5]) },
            };
        }
    }
}
