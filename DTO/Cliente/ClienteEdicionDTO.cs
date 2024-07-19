using System;
using System.Collections.Generic;
using System.Text;

namespace DTO.Cliente
{
    public class ClienteEdicionDTO
    {
        public Guid Id_Empresa { get; set; }

        public Guid Id_Cliente { get; set; }

        public string Nombre { get; set; }

        public string Apellido { get; set; }

        public int? Nro_Doc { get; set; }

        public string Tipo_Doc { get; set; }

        public string Estado_Civil { get; set; }

        public DateTime? Fecha_Nacimiento { get; set; }

        public string Sexo { get; set; }

        public string Email { get; set; }

        public string Nacionalidad { get; set; }

        public bool Estado { get; set; }
    }
}
