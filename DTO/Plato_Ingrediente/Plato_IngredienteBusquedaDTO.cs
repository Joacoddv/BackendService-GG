using DTO.Ingredientes;
using DTO.Plato;
using System;
using System.Collections.Generic;
using System.Text;

namespace DTO.Plato_Ingrediente
{
    public class Plato_IngredienteBusquedaDTO
    {
        public Guid Id_Empresa { get; set; }

        public Guid Id_Sucursal { get; set; }

        public Guid Id_PI { get; set; }

        public int Numero_PI { get; set; }

        public PlatoToListDTO Plato { get; set; }

        public IngredienteToListDTO Ingrediente { get; set; }

        public EBusquedaPlato_Ingrediente eBusquedaPlato_Ingrediente { get; set; }

    }
}
