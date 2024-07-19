using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dominio;

namespace DLL.Repositories.SqlServer.Adapters
{
  public sealed class ClienteAdapter
        {
            private readonly static ClienteAdapter _instance = new ClienteAdapter();

            public static ClienteAdapter Current
            {
                get
                {
                    return _instance;
                }
            }

            private ClienteAdapter()
            {
                //Implent here the initialization of your singleton
            }

            public Cliente Adapt(object[] values)
            {
            return new Cliente()
            {
                Id_Empresa = Guid.Parse(values[0].ToString()),
                Id_Sucursal = Guid.Parse(values[1].ToString()),
                Id_Cliente = Guid.Parse(values[2].ToString()),
                Numero_Cliente = Convert.ToInt32(values[3]),
                Nombre = values[4].ToString(),
                Apellido = values[5].ToString(),
                Nro_Doc = Convert.ToInt32(values[6]),
                Tipo_Doc = values[7]?.ToString().Trim(),
                Estado_Civil = values[8]?.ToString().Trim(),
                Fecha_Nacimiento = !string.IsNullOrEmpty(values[10]?.ToString()) ? Convert.ToDateTime(values[9]) : (DateTime?)null,
                Sexo = values[10]?.ToString().Trim(),
                Email = values[11].ToString(),
                Nacionalidad = values[12]?.ToString().Trim(),
                Fecha_Alta_Cliente = Convert.ToDateTime(values[13]),
                Estado = Convert.ToBoolean(values[14])
            };
        }
    }
}
