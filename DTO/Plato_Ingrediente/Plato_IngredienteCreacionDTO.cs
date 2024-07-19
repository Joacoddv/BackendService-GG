using DTO.Ingredientes;
using DTO.Plato;
using System;
using System.Collections.Generic;
using System.Text;

namespace DTO.Plato_Ingrediente
{
    public class Plato_IngredienteCreacionDTO
    {

        public Guid Id_Empresa { get; set; }

        public Guid Id_Sucursal { get; set; }


        public Guid Id_PI = Guid.NewGuid();

        public PlatoToListDTO Plato { get; set; }

        public IngredienteToListDTO Ingrediente { get; set; }

        public int Cantidad_Ingrediente { get; set; }

        //public Plato_IngredienteCreacionDTO()
        //{
        //    Id_PI = Guid.NewGuid();
        //}
    }
}
