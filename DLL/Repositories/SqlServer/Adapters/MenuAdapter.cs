using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dominio;

namespace DLL.Repositories.SqlServer.Adapters
{
    public sealed class MenuAdapter
    {
        private readonly static MenuAdapter _instance = new MenuAdapter();

        public static MenuAdapter Current
        {
            get
            {
                return _instance;
            }
        }

        private MenuAdapter()
        {
            //Implent here the initialization of your singleton
        }

        public Menu Adapt(object[] values)
        {
            if (values[4].ToString() == "" && values[5].ToString() == "")
            {
                return new Menu()
                {
                    Id_Empresa = Guid.Parse(values[0].ToString()),
                    Id_Sucursal = Guid.Parse(values[1].ToString()),
                    Id_Menu = Guid.Parse(values[2].ToString()),
                    Numero_Menu = Convert.ToInt32(values[3]),
                    Plato = new Plato { Id_Plato = Guid.Parse(values[6].ToString()) },
                    Estado = Convert.ToBoolean(values[7]),
                    Precio_Menu_Plato = Convert.ToDecimal(values[8]),
                    Observaciones = values[9].ToString(),

                };
            }
            else if (values[4].ToString() != "" && values[5].ToString() == "")
            {
                return new Menu()
                {
                    Id_Empresa = Guid.Parse(values[0].ToString()),
                    Id_Sucursal = Guid.Parse(values[1].ToString()),
                    Id_Menu = Guid.Parse(values[2].ToString()),
                    Numero_Menu = Convert.ToInt32(values[3]),
                    Fecha_Alta_Menu = Convert.ToDateTime(values[4]),
                    Plato = new Plato { Id_Plato = Guid.Parse(values[6].ToString()) },
                    Estado = Convert.ToBoolean(values[7]),
                    Precio_Menu_Plato = Convert.ToDecimal(values[8]),
                    Observaciones = values[9].ToString(),

                };
            }

            else if (values[4].ToString() == "" && values[5].ToString() != "")
            {
                return new Menu()
                {
                    Id_Empresa = Guid.Parse(values[0].ToString()),
                    Id_Sucursal = Guid.Parse(values[1].ToString()),
                    Id_Menu = Guid.Parse(values[2].ToString()),
                    Numero_Menu = Convert.ToInt32(values[3]),
                    Fecha_Dia_Menu = Convert.ToDateTime(values[5]),
                    Plato = new Plato { Id_Plato = Guid.Parse(values[6].ToString()) },
                    Estado = Convert.ToBoolean(values[7]),
                    Precio_Menu_Plato = Convert.ToDecimal(values[8]),
                    Observaciones = values[9].ToString(),

                };
            }
            else
            {
                return new Menu()
                {
                    Id_Empresa = Guid.Parse(values[0].ToString()),
                    Id_Sucursal = Guid.Parse(values[1].ToString()),
                    Id_Menu = Guid.Parse(values[2].ToString()),
                    Numero_Menu = Convert.ToInt32(values[3]),
                    Fecha_Alta_Menu = Convert.ToDateTime(values[4]),
                    Fecha_Dia_Menu = Convert.ToDateTime(values[5]),
                    Plato = new Plato { Id_Plato = Guid.Parse(values[6].ToString()) },
                    Estado = Convert.ToBoolean(values[7]),
                    Precio_Menu_Plato = Convert.ToDecimal(values[8]),
                    Observaciones = values[9].ToString(),

                };
            }

        }
    }
}
