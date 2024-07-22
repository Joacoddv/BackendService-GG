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
                Id_Empresa = values[0] != DBNull.Value ? Guid.Parse(values[0].ToString()) : Guid.Empty,
                Id_Sucursal = values[1] != DBNull.Value ? Guid.Parse(values[1].ToString()) : Guid.Empty,
                Id_Cliente = values[2] != DBNull.Value ? Guid.Parse(values[2].ToString()) : Guid.Empty,
                Numero_Cliente = values[3] != DBNull.Value ? Convert.ToInt32(values[3]) : (int?)null,
                Nombre = values[4] != DBNull.Value ? values[4].ToString() : null,
                Apellido = values[5] != DBNull.Value ? values[5].ToString() : null,
                Nro_Doc = values[6] != DBNull.Value ? Convert.ToInt32(values[6]) : (int?)null,
                Tipo_Doc = values[7] != DBNull.Value ? values[7].ToString().Trim() : null,
                Estado_Civil = values[8] != DBNull.Value ? values[8].ToString().Trim() : null,
                Fecha_Nacimiento = values[9] != DBNull.Value ? Convert.ToDateTime(values[9]) : (DateTime?)null,
                Sexo = values[10] != DBNull.Value ? values[10].ToString().Trim() : null,
                Email = values[11] != DBNull.Value ? values[11].ToString() : null,
                Nacionalidad = values[12] != DBNull.Value ? values[12].ToString().Trim() : null,
                Fecha_Alta_Cliente = (DateTime)(values[13] != DBNull.Value ? Convert.ToDateTime(values[13]) : (DateTime?)null),
                Estado = (bool)(values[14] != DBNull.Value ? Convert.ToBoolean(values[14]) : (bool?)null)
            };
        }

    }
}
