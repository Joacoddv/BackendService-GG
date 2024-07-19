using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dominio;

namespace DLL.Repositories.SqlServer.Adapters
{
  public sealed class Tipo_Transaccion_StockAdapter
    {
            private readonly static Tipo_Transaccion_StockAdapter _instance = new Tipo_Transaccion_StockAdapter();

            public static Tipo_Transaccion_StockAdapter Current
            {
                get
                {
                    return _instance;
                }
            }

            private Tipo_Transaccion_StockAdapter()
            {
                //Implent here the initialization of your singleton
            }

            public Tipo_Transaccion_Stock Adapt(object[] values)
            {
            return new Tipo_Transaccion_Stock()
            {
                Id_Empresa = Guid.Parse(values[0].ToString()),
                Id_Sucursal = Guid.Parse(values[1].ToString()),
                Id_Tipo_Transaccion_Stock = Guid.Parse(values[3].ToString()),
                Numero_Tipo_Transaccion_Stock = Convert.ToInt32(values[4]),
                Descripcion_Tipo_Transaccion_Stock = values[5].ToString(),

            };
        }
    }
}
