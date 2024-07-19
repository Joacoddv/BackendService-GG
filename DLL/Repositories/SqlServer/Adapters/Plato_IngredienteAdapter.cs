using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dominio;

namespace DLL.Repositories.SqlServer.Adapters
{
    public sealed class Plato_IngredienteAdapter
    {
        private readonly static Plato_IngredienteAdapter _instance = new Plato_IngredienteAdapter();

        public static Plato_IngredienteAdapter Current
        {
            get
            {
                return _instance;
            }
        }

        private Plato_IngredienteAdapter()
        {
            //Implent here the initialization of your singleton
        }

        public Plato_Ingrediente Adapt(object[] values)
        {
            return new Plato_Ingrediente()
            {
                Id_Empresa = Guid.Parse(values[0].ToString()),
                Id_Sucursal = Guid.Parse(values[1].ToString()),
                Id_PI = Guid.Parse(values[2].ToString()),
                Numero_PI = Convert.ToInt32(values[3]),
                Plato = new Plato { Id_Plato = Guid.Parse(values[4].ToString()) },
                Ingrediente = new Ingrediente { Id_Ingrediente = Guid.Parse(values[5].ToString()) },
                Cantidad_Ingrediente = Convert.ToInt32(values[6]),

            };
        }
    }
}
