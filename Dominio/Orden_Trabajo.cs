using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio
{
    public class Orden_Trabajo
    {
        public Guid Id_Empresa { get; set; }

        public Guid Id_Sucursal { get; set; }

        public Guid Id_Orden_Trabajo { get; set; }

        public int Numero_Orden { get; set; }

        public Pedido Pedido { get; set; }

        public Plato Plato { get; set; }

        public EEstadoOT EEstadoOT { get; set; }

        public int Cantidad { get; set; }

        public string Observaciones { get; set; }

        public DateTime Fecha_Creacion { get; set; }

        public DateTime Fecha_Modificacion { get; set; }
        public Orden_Trabajo(Guid id_Empresa, Guid id_Sucursal, Guid id_orden_trabajo, int numero_orden, Pedido pedido, Plato plato, EEstadoOT estadoOT, int cantidad, string observaciones, DateTime fecha_creacion, DateTime fecha_modificacion)
        {
            Id_Empresa = id_Empresa;
            Id_Sucursal = id_Sucursal;
            Id_Orden_Trabajo = id_orden_trabajo;
            Numero_Orden = numero_orden;
            Pedido = pedido;
            Plato = plato;
            EEstadoOT = estadoOT;
            Cantidad = cantidad;
            Observaciones = observaciones;
            Fecha_Creacion = fecha_creacion;
            Fecha_Modificacion = fecha_modificacion;
        }

        public Orden_Trabajo()
        {

        }
    }
}
