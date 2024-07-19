using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio
{
    public class Cliente
    {
        public Guid Id_Empresa { get; set; }

        public Guid Id_Sucursal { get; set; }

        public Guid Id_Cliente { get; set; }

        public int? Numero_Cliente { get; set; }

        public string Nombre { get; set; }

        public string Apellido { get; set; }

        public int? Nro_Doc { get; set; }

        public string Tipo_Doc { get; set; }

        public string Estado_Civil { get; set; }

        public DateTime? Fecha_Nacimiento { get; set; }

        public string Sexo { get; set; }

        public string Email { get; set; }

        public string Nacionalidad { get; set; }

        public DateTime Fecha_Alta_Cliente { get; set; }

        public bool Estado { get; set; }


        public Cliente(Guid id_Empresa, Guid id_Sucursal,Guid id_cliente, int numero_cliente, string nombre, string apellido, int nro_doc, string tipo_doc, string estado_civil, DateTime fecha_nacimiento, string sexo, string email, string nacionalidad, int numero_direccion, DateTime fecha_alta_cliente, bool estado)
        {
            Id_Empresa = id_Empresa;
            Id_Sucursal = id_Sucursal;
            Id_Cliente = id_cliente;
            Numero_Cliente = numero_cliente;
            Nombre = nombre;
            Apellido = apellido;
            Nro_Doc = nro_doc;
            Tipo_Doc = tipo_doc;
            Estado_Civil = estado_civil;
            Fecha_Nacimiento = fecha_nacimiento;
            Sexo = sexo;
            Email = email;
            Nacionalidad = nacionalidad;
            Fecha_Alta_Cliente = fecha_alta_cliente;
            Estado = estado;
        }

        public Cliente ()
        {

        }

    }
}
