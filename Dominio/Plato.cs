using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio
{
    public class Plato
    {
        public Guid Id_Empresa { get; set; }

        public Guid Id_Sucursal { get; set; }

        public Guid Id_Plato { get; set; }

        public int Numero_Plato { get; set; }

        public string Nombre_Plato { get; set; }

        public string Descripcion { get; set; }

        public bool Estado { get; set; }

        public Plato(Guid id_empresa, Guid id_sucursal,Guid id_plato, int numero_plato, string nombre_plato, string descripcion, bool estado)
        {
            Id_Empresa = id_empresa;
            Id_Sucursal = id_sucursal;
            Id_Plato = id_plato;
            Numero_Plato = numero_plato;
            Nombre_Plato = nombre_plato;
            Descripcion = descripcion;
            Estado = estado;
        }

        public Plato()
        { }

        public static implicit operator List<object>(Plato v)
        {
            throw new NotImplementedException();
        }
    }
}
