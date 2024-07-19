using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dominio;

namespace DLL.Repositories.SqlServer.Adapters
{
    public sealed class DireccionesAdapter
    {
        private readonly static DireccionesAdapter _instance = new DireccionesAdapter();

        public static DireccionesAdapter Current
        {
            get
            {
                return _instance;
            }
        }

        private DireccionesAdapter()
        {
            //Implent here the initialization of your singleton
        }

        public Direccion Adapt(object[] values)
        {
            return new Direccion()
            {
                Id_Empresa= Guid.Parse(values[0].ToString()),
                Id_Sucursal = Guid.Parse(values[1].ToString()),
                Id_Direccion = Guid.Parse(values[2].ToString()),
                Numero_Direccion = Convert.ToInt32(values[4]),
                Cliente = new Cliente { Id_Cliente = (Guid)(!string.IsNullOrEmpty(values[3]?.ToString()) ? Guid.Parse(values[3].ToString()) : (Guid?)null),
                Numero_Cliente = Convert.ToInt32(values[5])},
                Tipo_Direccion = values[6].ToString(),
                Telefono_Cel = values[7].ToString(),
                Telefono_Casa = values[8].ToString(),
                Telefono_Otro = values[9].ToString(),
                Nombre_Calle = values[10].ToString(),
                Altura = Convert.ToInt32(values[11]),
                Piso = values[12].ToString(),
                Localidad = values[13].ToString()

            };
        }
    }
}
