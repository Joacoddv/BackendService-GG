using System;
using System.Collections.Generic;
using System.Text;

namespace DTO.Plato
{
    public class PlatoCreacionDTO
    {
        public Guid Id_Empresa { get; set; }

        public Guid Id_Sucursal { get; set; }

        public Guid Id_Plato { get; set; }

        public string Nombre_Plato { get; set; }

        public string Descripcion { get; set; }

        public bool Estado { get; set; }

        public PlatoCreacionDTO()
        { 
            Id_Plato = Guid.NewGuid();
            Estado = true;
        }
    }
}
