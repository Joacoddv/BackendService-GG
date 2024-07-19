using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio
{
    public class Plato_Precio
    {
        public Guid Id_Empresa { get; set; }

        public Guid Id_Sucursal { get; set; }

        public Guid Id_Plato_Precio { get; set; }

        public int Numero_Plato_Precio { get; set; }

        public Plato Plato { get; set; }

        public DateTime Fecha_Desde { get; set; }

        public DateTime Fecha_Hasta { get; set; }

        public DateTime Fecha_Create { get; set; }

        public decimal Precio { get; set; }
        public Plato_Precio(Guid id_empresa, Guid id_sucursal, Guid id_plato_precio, int numero_plato_precio, Plato plato, DateTime fecha_desde, DateTime fecha_hasta, DateTime fecha_create, decimal precio)
        {
            Id_Empresa = id_empresa;
            Id_Sucursal = id_sucursal;
            Id_Plato_Precio = id_plato_precio;
            Numero_Plato_Precio = numero_plato_precio;
            Plato = plato;
            Fecha_Desde = fecha_desde;
            Fecha_Hasta = fecha_hasta;
            Fecha_Create = fecha_create;
            Precio = precio;
        }

        public Plato_Precio()
        {

        }
    }
}
