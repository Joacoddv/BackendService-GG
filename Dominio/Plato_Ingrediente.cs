using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio
{
    public class Plato_Ingrediente
    {
        public Guid Id_Empresa { get; set; }

        public Guid Id_Sucursal { get; set; }

        public Guid Id_PI { get; set; }

        public int Numero_PI { get; set; }

        public Plato Plato { get; set; }

        public Ingrediente Ingrediente { get; set; }

        public int Cantidad_Ingrediente { get; set; }

        public Plato_Ingrediente(Guid id_empresa, Guid id_sucursal,Guid id_pi, int numero_pi, Plato plato, Ingrediente ingrediente, int cantidad_ingrediente)
        {
            Id_Empresa = id_empresa;
            Id_Sucursal = id_sucursal;
            Id_PI = id_pi;
            Numero_PI = numero_pi;
            Plato = plato;
            Ingrediente = ingrediente;
            Cantidad_Ingrediente = cantidad_ingrediente;
        }

        public Plato_Ingrediente()
        {

        }
    }
}
