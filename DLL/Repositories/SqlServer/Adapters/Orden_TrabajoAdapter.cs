using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dominio;

namespace DLL.Repositories.SqlServer.Adapters
{
    public sealed class Orden_TrabajoAdapter
    {
        private readonly static Orden_TrabajoAdapter _instance = new Orden_TrabajoAdapter();

        public static Orden_TrabajoAdapter Current
        {
            get
            {
                return _instance;
            }
        }

        private Orden_TrabajoAdapter()
        {
            //Implent here the initialization of your singleton
        }

        public Orden_Trabajo Adapt(object[] values)
        {
            return new Orden_Trabajo()
            {
                Id_Empresa = Guid.Parse(values[0].ToString()),
                Id_Sucursal = Guid.Parse(values[1].ToString()),
                Id_Orden_Trabajo = Guid.Parse(values[2].ToString()),
                Numero_Orden = Convert.ToInt32(values[3]),
                Pedido = new Pedido {Id_Pedido = (Guid)(!string.IsNullOrEmpty(values[4]?.ToString()) ? Guid.Parse(values[4].ToString()) : (Guid?)null), },
                Plato = new Plato { Id_Plato = (Guid)(!string.IsNullOrEmpty(values[5]?.ToString()) ? Guid.Parse(values[5].ToString()) : (Guid?)null), },
                EEstadoOT = (EEstadoOT)Convert.ToInt32(values[6]),
                Cantidad = Convert.ToInt32(values[7]),
                Observaciones = values[8].ToString(),
                Fecha_Creacion= (DateTime)(!string.IsNullOrEmpty(values[9]?.ToString()) ? Convert.ToDateTime(values[9]) : (DateTime?)null),
                Fecha_Modificacion= (DateTime)(!string.IsNullOrEmpty(values[10]?.ToString()) ? Convert.ToDateTime(values[10]) : (DateTime?)null),
        };
        }
    }
}
