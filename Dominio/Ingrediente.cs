using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio
{
    public class Ingrediente
    {
        public Guid Id_Empresa { get; set; }

        public Guid Id_Sucursal { get; set; }

        public Guid Id_Ingrediente { get; set; }

        public int Numero_ingrediente { get; set; }

        public string Nombre_Ingrediente { get; set; }

        public string Descripcion { get; set; }

        public string Medida { get; set; }

        public bool Estado { get; set; }

        public Ingrediente(Guid id_Empresa, Guid id_Sucursal, Guid id_ingrediente, int numero_ingrediente, string nombre_ingrediente, string descripcion, string medida, bool estado)
        {
            Id_Empresa = id_Empresa;
            Id_Sucursal = id_Sucursal;
            Id_Ingrediente = id_ingrediente;
            Numero_ingrediente = numero_ingrediente;
            Nombre_Ingrediente = nombre_ingrediente;
            Descripcion = descripcion;
            Medida = medida;
            Estado = estado;

        }

        public Ingrediente()
        { }
    }
}
