using DTO.Pedido;
using DTO.Plato;
using System;
using System.Collections.Generic;
using System.Text;

namespace DTO.Plato_Pedido
{
    public class Plato_PedidoCreacionDTO
    {
        public Guid Id_Empresa { get; set; }

        public Guid Id_Sucursal { get; set; }

        public Guid Id_Plato_Pedido { get; set; } = Guid.NewGuid();

        public PlatoToListDTO Plato { get; set; }

        public PedidoToListDTO Pedido { get; set; }

        public int Cantidad { get; set; }

        public string Observaciones { get; set; }

        public Plato_PedidoCreacionDTO()
        {
            Id_Plato_Pedido = Guid.NewGuid();
        }
    }
}
