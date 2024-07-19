using DTO.Plato;
using System;
using System.Collections.Generic;
using System.Text;

namespace DTO.Plato_Precio
{
    public class Plato_PrecioCreacionDTO
    {
        public Guid Id_Empresa { get; set; }

        public Guid Id_Sucursal { get; set; }

        public Guid Id_Plato_Precio { get; set; }

        public PlatoToListDTO Plato { get; set; }

        public DateTime Fecha_Desde { get; set; }

        public DateTime Fecha_Hasta { get; set; }

        public DateTime Fecha_Create { get; set; } = DateTime.Now;

        public decimal Precio { get; set; }

        public Plato_PrecioCreacionDTO() 
        {
            Id_Plato_Precio = Guid.NewGuid();
        }
    }
}
