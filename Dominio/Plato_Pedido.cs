using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio
{
    public class Plato_Pedido
    {
        public Guid Id_Empresa { get; set; }

        public Guid Id_Sucursal { get; set; }

        public Guid Id_Plato_Pedido { get; set; }

        public int Numero_Plato_Pedido { get; set; }

        public Plato Plato { get; set; }

        public Pedido Pedido { get; set; }

        public int Cantidad { get; set; }

        public string Observaciones { get; set; }

        public Plato_Pedido(Guid id_empresa, Guid id_sucursal, Guid id_plato_pedido, int numero_plato_pedido, Plato plato, Pedido pedido, int cantidad,string observaciones)
        {
            Id_Empresa = id_empresa;
            Id_Sucursal = id_sucursal;
            Id_Plato_Pedido = id_plato_pedido;
            Numero_Plato_Pedido = numero_plato_pedido;
            Plato = plato;
            Pedido = pedido;
            Cantidad = cantidad;
            Observaciones = observaciones;
        }

        public Plato_Pedido()
        {

        }
    }
}
