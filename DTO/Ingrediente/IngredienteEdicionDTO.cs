using System;
using System.Collections.Generic;
using System.Text;

namespace DTO.Ingredientes
{
    public class IngredienteEdicionDTO
    {
        public Guid Id_Empresa { get; set; }

        public Guid Id_Sucursal { get; set; }

        public Guid Id_Ingrediente { get; set; }

        public string Nombre_Ingrediente { get; set; }

        public string Descripcion { get; set; }

        public string Medida { get; set; }

        public bool Estado { get; set; }
    }
}
