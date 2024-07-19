using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio
{
    public class Tipo_Transaccion_Stock
    {
        public Guid Id_Empresa { get; set; }

        public Guid Id_Sucursal { get; set; }

        public Guid Id_Tipo_Transaccion_Stock { get; set; }

        public int Numero_Tipo_Transaccion_Stock { get; set; }

        public string Descripcion_Tipo_Transaccion_Stock { get; set; }

        public Tipo_Transaccion_Stock(Guid id_empresa, Guid id_sucursal, Guid id_tipo_transaccion_stock, int numero_tipo_transaccion_stock, string descripcion_tipo_transaccion_stock)
        {
            Id_Empresa = id_empresa;
            Id_Sucursal = id_sucursal;
            Id_Tipo_Transaccion_Stock = id_tipo_transaccion_stock;
            Numero_Tipo_Transaccion_Stock = numero_tipo_transaccion_stock;
            Descripcion_Tipo_Transaccion_Stock = descripcion_tipo_transaccion_stock;
        }

        public Tipo_Transaccion_Stock()
        {

        }
    }
}
