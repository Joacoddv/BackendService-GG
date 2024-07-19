using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dominio;

namespace DLL.Repositories.SqlServer.Adapters
{
    public sealed class Transaccion_StockAdapter
    {
        private readonly static Transaccion_StockAdapter _instance = new Transaccion_StockAdapter();

        public static Transaccion_StockAdapter Current
        {
            get
            {
                return _instance;
            }
        }

        private Transaccion_StockAdapter()
        {
            //Implent here the initialization of your singleton
        }

        public Transaccion_Stock Adapt(object[] values)
        {
            return new Transaccion_Stock()
            {
                Id_Empresa = Guid.Parse(values[0].ToString()),
                Id_Sucursal = Guid.Parse(values[1].ToString()),
                Id_Transaccion_Stock = Guid.Parse(values[2].ToString()),
                Tipo_Transaccion_Stock = new  Tipo_Transaccion_Stock { Id_Tipo_Transaccion_Stock =  Guid.Parse(values[3].ToString()) },
                Fecha_Transaccion = (DateTime)(!string.IsNullOrEmpty(values[4]?.ToString()) ? Convert.ToDateTime(values[4]) : (DateTime?)null),
                Orden_Trabajo = new Orden_Trabajo { Id_Orden_Trabajo = (Guid)(!string.IsNullOrEmpty(values[5]?.ToString()) ? Guid.Parse(values[5].ToString()) : (Guid?)null), },
                Ingrediente = new Ingrediente { Id_Ingrediente = (Guid)(!string.IsNullOrEmpty(values[6]?.ToString()) ? Guid.Parse(values[6].ToString()) : (Guid?)null), },
                Cantidad = Convert.ToInt32(values[7]),
                Cantidad_Restante = Convert.ToInt32(values[8]),

            };
        }
    }
}
