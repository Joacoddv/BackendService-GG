using System;
using System.Collections.Generic;
using System.Text;

namespace DTO.Cliente
{
    public class ClienteCreacionDTO
    {
        public Guid Id_Empresa { get; set; }

        public Guid Id_Sucursal { get; set; }

        public Guid Id_Cliente { get; set; } = Guid.NewGuid();

        public string Nombre { get; set; }

        public string Apellido { get; set; }

        public int? Nro_Doc { get; set; }

        public string Tipo_Doc { get; set; }

        public string Estado_Civil { get; set; }

        public DateTime? Fecha_Nacimiento { get; set; }

        public string Sexo { get; set; }

        public string Email { get; set; }

        public string Nacionalidad { get; set; }

        public DateTime Fecha_Alta_Cliente { get; set; } = DateTime.Now;

        public bool Estado { get; set; } = true;

        public ClienteCreacionDTO()
        {
            Id_Cliente = Guid.NewGuid();
            Fecha_Alta_Cliente = DateTime.Now;
            Estado = true;
        }
    }
}
