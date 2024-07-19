using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dominio;

namespace DLL.Repositories.SqlServer.Adapters
{
    public sealed class IngredienteAdapter
    {
        private readonly static IngredienteAdapter _instance = new IngredienteAdapter();

        public static IngredienteAdapter Current
        {
            get
            {
                return _instance;
            }
        }

        private IngredienteAdapter()
        {
            //Implent here the initialization of your singleton
        }

        public Ingrediente Adapt(object[] values)
        {
            return new Ingrediente()
            {
                Id_Empresa = Guid.Parse(values[0].ToString()),
                Id_Sucursal = Guid.Parse(values[1].ToString()),
                Id_Ingrediente = Guid.Parse(values[2].ToString()),
                Numero_ingrediente = Convert.ToInt32(values[3]),
                Nombre_Ingrediente = values[4].ToString(),
                Descripcion = values[5].ToString(),
                Medida = values[6].ToString(),
                Estado = Convert.ToBoolean(values[7])
            };
        }
    }
}
