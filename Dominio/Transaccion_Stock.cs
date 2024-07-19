using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio
{
    public class Transaccion_Stock
    {

        public Guid Id_Empresa { get; set; }

        public Guid Id_Sucursal { get; set; }

        public Guid Id_Transaccion_Stock { get; set; }

        public Tipo_Transaccion_Stock Tipo_Transaccion_Stock { get; set; }

        public DateTime Fecha_Transaccion { get; set; }

        public Orden_Trabajo Orden_Trabajo { get; set; }

        public Ingrediente Ingrediente { get; set; }

        public int Cantidad { get; set; }

        public int Cantidad_Restante { get; set; }

        public Transaccion_Stock(Guid id_empresa, Guid id_sucursal, Guid id_transaccion_stock, Tipo_Transaccion_Stock tipo_transaccion_stock, DateTime  fecha_transaccion, Orden_Trabajo orden_trabajo, Ingrediente ingrediente, int cantidad, int cantidad_restante)
        {
            Id_Empresa = id_empresa;
            Id_Sucursal = id_sucursal;
            Id_Transaccion_Stock = id_transaccion_stock;
            Tipo_Transaccion_Stock = tipo_transaccion_stock;
            Fecha_Transaccion = fecha_transaccion;
            Orden_Trabajo = orden_trabajo;
            Ingrediente = ingrediente;
            Cantidad = cantidad;
            Cantidad_Restante = cantidad_restante;

        }

        public Transaccion_Stock()
        {

        }
    }
}
