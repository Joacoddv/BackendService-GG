using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dominio;

namespace DLL.Repositories.SqlServer.Adapters
{
    public sealed class PlatoAdapter
    {
        private readonly static PlatoAdapter _instance = new PlatoAdapter();

        public static PlatoAdapter Current
        {
            get
            {
                return _instance;
            }
        }

        private PlatoAdapter()
        {
            //Implent here the initialization of your singleton
        }

        public Plato Adapt(object[] values)
        {
            return new Plato()
            {
                Id_Empresa = Guid.Parse(values[0].ToString()),
                Id_Sucursal = Guid.Parse(values[1].ToString()),
                Id_Plato = Guid.Parse(values[2].ToString()),
                Numero_Plato = Convert.ToInt32(values[3]),
                Nombre_Plato = values[4].ToString(),
                Descripcion = values[5].ToString(),
                Estado = Convert.ToBoolean(values[6]),

            };
        }
    }
}
