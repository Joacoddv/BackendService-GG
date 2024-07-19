using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio
{
    public class Direccion
    {
        public Guid Id_Empresa { get; set; }

        public Guid Id_Sucursal { get; set; }

        public Guid Id_Direccion { get; set; }

        public int Numero_Direccion { get; set; }

        public Cliente Cliente { get; set; }

        public string Tipo_Direccion { get; set; }

        public string Telefono_Cel { get; set; }

        public string Telefono_Casa { get; set; }

        public string Telefono_Otro { get; set; }

        public string Nombre_Calle { get; set; }

        public int Altura { get; set; }

        public string Piso { get; set; }

        public string Localidad { get; set; }


        public Direccion(Guid id_Empresa, Guid id_Sucursal, Guid id_direccion, int numero_direccion, Cliente cliente,string tipo_direccion, string telefono_cel, string telefono_casa, string telefono_otro, string nombre_calle, int altura, string piso, string localidad)
        {
            Id_Empresa = id_Empresa;
            Id_Sucursal = id_Sucursal;
            Id_Direccion = id_direccion;
            Numero_Direccion = numero_direccion;
            Cliente = cliente;
            Tipo_Direccion = tipo_direccion;
            Telefono_Cel = telefono_cel;
            Telefono_Casa = telefono_casa;
            Telefono_Otro = telefono_otro;
            Nombre_Calle = nombre_calle;
            Altura = altura;
            Piso = piso;
            Localidad = localidad;
        }

        public Direccion()
        { }

    }
}
