using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dominio
{
    public class Menu
    {
        public Guid Id_Empresa { get; set; }

        public Guid Id_Sucursal { get; set; }

        public Guid Id_Menu { get; set; }

        public int Numero_Menu { get; set; }

        public DateTime Fecha_Alta_Menu { get; set; }

        public DateTime Fecha_Dia_Menu { get; set; }

        public Plato Plato { get; set; }

        public bool Estado { get; set; }

        public decimal Precio_Menu_Plato { get; set; }

        public String Observaciones { get; set; }
        public Menu(Guid id_empresa, Guid id_sucursal, Guid id_menu, int numero_menu, DateTime fecha_alta_menu, DateTime fecha_dia_menu, Plato plato, bool estado, decimal precio_menu_plato, string observaciones)
        {
            Id_Empresa = id_empresa;
            Id_Sucursal = id_sucursal;
            Id_Menu = id_menu;
            Numero_Menu = numero_menu;
            Fecha_Alta_Menu = fecha_alta_menu;
            Fecha_Dia_Menu = fecha_dia_menu;
            Plato = plato;
            Estado = estado;
            Precio_Menu_Plato = precio_menu_plato;
            Observaciones = observaciones;

        }


        public Menu()
        { }
    }
}
