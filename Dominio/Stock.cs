using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio
{
    public class Stock
    {

        public Guid Id_Empresa { get; set; }

        public Guid Id_Sucursal { get; set; }

        public Guid Id_Stock { get; set; }

        public int Numero_Stock { get; set; }

        public Ingrediente Ingrediente { get; set; }

        public int Cantidad { get; set; }


        public Stock(Guid id_Empresa, Guid id_Sucursal ,Guid id_Stock, int numero_Stock, Ingrediente ingrediente, int cantidad)
        {
            Id_Empresa = id_Empresa;
            Id_Sucursal = id_Sucursal;
            Id_Stock = id_Stock;
            Numero_Stock = numero_Stock;
            Ingrediente = ingrediente;
            Cantidad = cantidad;
        }

        public Stock()
        {

        }
    }
}
