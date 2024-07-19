using DTO.Plato;
using System;
using System.Collections.Generic;
using System.Text;

namespace DTO.Menu
{
    public class MenuCreacionDTO
    {
        public Guid Id_Empresa { get; set; }

        public Guid Id_Sucursal { get; set; }

        public Guid Id_Menu { get; set; }

        public DateTime Fecha_Alta_Menu { get; set; }

        public DateTime Fecha_Dia_Menu { get; set; }

        public PlatoToListDTO Plato { get; set; }

        public bool Estado { get; set; }

        public decimal Precio_Menu_Plato { get; set; }

        public String Observaciones { get; set; }

        public MenuCreacionDTO()
        { 
            Id_Menu = Guid.NewGuid();
            Fecha_Alta_Menu = DateTime.Now;
            Estado = true;

        }
    }
}
