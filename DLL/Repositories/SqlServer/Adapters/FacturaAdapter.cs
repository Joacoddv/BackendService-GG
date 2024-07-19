using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dominio;

namespace DLL.Repositories.SqlServer.Adapters
{
    public sealed class FacturaAdapter
    {
        private readonly static FacturaAdapter _instance = new FacturaAdapter();

        public static FacturaAdapter Current
        {
            get
            {
                return _instance;
            }
        }

        private FacturaAdapter()
        {
            //Implent here the initialization of your singleton
        }

        public Factura Adapt(object[] values)
        {
            return new Factura()
            {
                Id_Empresa = Guid.Parse(values[0].ToString()),
                Id_Sucursal = Guid.Parse(values[1].ToString()),
                Id_Factura = Guid.Parse(values[2].ToString()),
                Numero_Factura = Convert.ToInt32(values[3]),
                Fecha_Alta_Factura = Convert.ToDateTime(values[4]),
                Cliente = new Cliente { Id_Cliente = (Guid)(!string.IsNullOrEmpty(values[5]?.ToString()) ? Guid.Parse(values[5].ToString()) : (Guid?)null), },
                Estado = (EEstadoFactura)Convert.ToInt32(values[6]),
                Sub_Total = Convert.ToDecimal(values[7]),
                Total_Iva = Convert.ToDecimal(values[8]),
                Total_Factura = Convert.ToDecimal(values[9])
            };
        }
    }
}
