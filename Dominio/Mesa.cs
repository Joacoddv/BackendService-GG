using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio
{
    public class Mesa
    {
        public Guid Id_Empresa { get; set; }

        public Guid Id_Sucursal { get; set; }

        public Guid Id_Mesa { get; set; }

        public int Numero_Mesa { get; set; }

        public string  Ubicacion_Mesa { get; set; }

        public int Cantidad { get; set; }


        public Mesa(Guid id_Empresa, Guid id_Sucursal, Guid id_mesa, int numero_mesa, string ubicacion_mesa, int cantidad)
        {
            Id_Empresa = id_Empresa;
            Id_Sucursal = id_Sucursal;
            Id_Mesa = id_mesa;
            Numero_Mesa = numero_mesa;
            Ubicacion_Mesa = ubicacion_mesa;
            Cantidad = cantidad;
        }

        public Mesa()
        {

        }
    }
}
