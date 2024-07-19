using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dominio;

namespace DLL.Repositories.SqlServer.Adapters
{
    public sealed class Plato_PrecioAdapter
    {
        private readonly static Plato_PrecioAdapter _instance = new Plato_PrecioAdapter();

        public static Plato_PrecioAdapter Current
        {
            get
            {
                return _instance;
            }
        }

        private Plato_PrecioAdapter()
        {
            //Implent here the initialization of your singleton
        }

        public Plato_Precio Adapt(object[] values)
        {
            return new Plato_Precio()
            {
                Id_Empresa = Guid.Parse(values[0].ToString()),
                Id_Sucursal = Guid.Parse(values[1].ToString()),
                Id_Plato_Precio = Guid.Parse(values[2].ToString()),
                Numero_Plato_Precio = Convert.ToInt32(values[3]),
                Plato = new Plato { Id_Plato = Guid.Parse(values[4].ToString()), },
                Fecha_Desde = Convert.ToDateTime(values[5]),
                Fecha_Hasta = Convert.ToDateTime(values[6]),
                Fecha_Create = Convert.ToDateTime(values[7]),
                Precio = Convert.ToDecimal(values[8]),
            };
        }
    }
}
