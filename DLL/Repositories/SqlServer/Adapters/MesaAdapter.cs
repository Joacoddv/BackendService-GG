using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dominio;

namespace DLL.Repositories.SqlServer.Adapters
{
    public sealed class MesaAdapter
    {
        private readonly static MesaAdapter _instance = new MesaAdapter();

        public static MesaAdapter Current
        {
            get
            {
                return _instance;
            }
        }

        private MesaAdapter()
        {
            //Implent here the initialization of your singleton
        }

        public Mesa Adapt(object[] values)
        {
            return new Mesa()
            {
                Id_Empresa = Guid.Parse(values[0].ToString()),
                Id_Sucursal = Guid.Parse(values[1].ToString()),
                Id_Mesa = Guid.Parse(values[2].ToString()),
                Numero_Mesa = Convert.ToInt32(values[3]),
                Ubicacion_Mesa = (values[4]).ToString(),
                Cantidad = Convert.ToInt32(values[5]),
        };
        }
    }
}
