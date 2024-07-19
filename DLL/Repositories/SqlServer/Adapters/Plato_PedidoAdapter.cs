using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dominio;

namespace DLL.Repositories.SqlServer.Adapters
{
    public sealed class Plato_PedidoAdapter
    {
        private readonly static Plato_PedidoAdapter _instance = new Plato_PedidoAdapter();

        public static Plato_PedidoAdapter Current
        {
            get
            {
                return _instance;
            }
        }

        private Plato_PedidoAdapter()
        {
            //Implent here the initialization of your singleton
        }

        public Plato_Pedido Adapt(object[] values)
        {
            return new Plato_Pedido()
            {
                Id_Empresa = Guid.Parse(values[0].ToString()),
                Id_Sucursal = Guid.Parse(values[1].ToString()),
                Id_Plato_Pedido = Guid.Parse(values[2].ToString()),
                Numero_Plato_Pedido = Convert.ToInt32(values[3]),
                Pedido = new Pedido { Id_Pedido = Guid.Parse(values[4].ToString()) },
                Plato = new Plato { Id_Plato = Guid.Parse(values[5].ToString()) },
                Cantidad = Convert.ToInt32(values[6]),
                Observaciones = values[7].ToString(),
            };
        }
    }
}
