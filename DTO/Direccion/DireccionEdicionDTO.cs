using DTO.Cliente;
using System;
using System.Collections.Generic;
using System.Text;

namespace DTO.Direcciones
{
    public class DireccionEdicionDTO
    {
        public Guid Id_Empresa { get; set; }

        public Guid Id_Sucursal { get; set; }

        public Guid Id_Direccion { get; set; }

        public int Numero_Direccion { get; set; }

        public ClienteBusquedaDTO ClienteBusquedaDTO { get; set; }

        public string Tipo_Direccion { get; set; }

        public string Telefono_Cel { get; set; }

        public string Telefono_Casa { get; set; }

        public string Telefono_Otro { get; set; }

        public string Nombre_Calle { get; set; }

        public int Altura { get; set; }

        public string Piso { get; set; }

        public string Localidad { get; set; }
    }
}
