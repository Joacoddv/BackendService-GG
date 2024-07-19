using DTO.Plato;
using System;
using System.Collections.Generic;
using System.Text;

namespace DTO.Menu
{
    public class MenuEdicionDTO
    {
        public Guid Id_Empresa { get; set; }

        public Guid Id_Sucursal { get; set; }

        public Guid Id_Menu { get; set; }

        public int Numero_Menu { get; set; }

        public DateTime Fecha_Dia_Menu { get; set; }

        public PlatoToListDTO Plato { get; set; }

        public bool Estado { get; set; }

        public decimal Precio_Menu_Plato { get; set; }

        public String Observaciones { get; set; }

    }
}
