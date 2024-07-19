using System;
using System.Collections.Generic;
using System.Text;

namespace DTO.Plato
{
    public class PlatoEdicionDTO
    {
        public Guid Id_Empresa { get; set; }

        public Guid Id_Sucursal { get; set; }

        public Guid Id_Plato { get; set; }

        public int Numero_Plato { get; set; }

        public string Nombre_Plato { get; set; }

        public string Descripcion { get; set; }

        public bool Estado { get; set; }

    }
}
