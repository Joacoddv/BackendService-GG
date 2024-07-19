using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dominio;

namespace DLL.Repositories.SqlServer.Adapters
{
    public sealed class StockAdapter

    {
        private readonly static StockAdapter _instance = new StockAdapter();

        public static StockAdapter Current
        {
            get
            {
                return _instance;
            }
        }

        private StockAdapter()
        {
            //Implent here the initialization of your singleton
        }

        public Stock Adapt(object[] values)
        {
            return new Stock()
            {
                Id_Empresa = Guid.Parse(values[0].ToString()),
                Id_Sucursal = Guid.Parse(values[1].ToString()),
                Id_Stock = Guid.Parse(values[3].ToString()),
                Numero_Stock = Convert.ToInt32(values[4]),
                Ingrediente = new Ingrediente { Id_Ingrediente = (Guid)(!string.IsNullOrEmpty(values[5]?.ToString()) ? Guid.Parse(values[5].ToString()) : (Guid?)null), },
                Cantidad = Convert.ToInt32(values[6]),
            };
        }
    }
}
