using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BLL.Contracts;
using DAL.Contracts;
using DAL.Factories;
using Dominio;
using Servicios.Services;

namespace BLL
{
    public sealed class MenuBusinessLogic : IGenericBusinessLogic<Menu>
    {
        private readonly static MenuBusinessLogic _instance = new MenuBusinessLogic();

        IGenericRepository<Menu> MenuRepository = Factory.Current.GetMenuRepository();

        List<Menu> Menus = new List<Menu>();
        public static MenuBusinessLogic Current
        {
            get
            {
                return _instance;
            }
        }

        private MenuBusinessLogic()
        {
            //Implent here the initialization of your singleton
        }
        public void Add(Menu obj)
        {
            //Doy de alta un menu
            try
            {
                LoggerManager.Current.Write($"BLL Menu - Validando alta de menú", EventLevel.Informational);
                Menus = GetAll(obj).ToList();

                if ((from o in Menus
                     where o.Plato.Id_Plato == obj.Plato.Id_Plato && o.Fecha_Dia_Menu == obj.Fecha_Dia_Menu
                     select o).Any() == true)
                {
                    throw new Exception("Ya existe un Menu del dia con este plato.");
                }
                else
                {
                    MenuRepository.Insert(obj);
                    Menus = GetAll(obj).ToList();
                }


            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Menu: - Error al dar de alta menú: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }

        public IEnumerable<Menu> GetAll(Menu obj)
        {
            //Listo todos los platos en orden descendente
            LoggerManager.Current.Write($"BLL Menu - Validando listar menú", EventLevel.Informational);
            try
            {
                return from o in MenuRepository.GetAll(obj)
                       orderby o.Numero_Menu descending
                       select o;
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Menu - Error al listar menú: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }

        public Menu GetOne(Menu obj)
        {
            //Busco un plato por su ID
            LoggerManager.Current.Write($"BLL Menu - Validando buscar menú por ID menú", EventLevel.Informational);
            try
            {
                return MenuRepository.GetOne(obj);
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Menu - Error al buscar menú por ID menú: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }

        public void Remove(Menu obj)
        {
            //Remuevo un plato a partir de su ID
            LoggerManager.Current.Write($"BLL Menu - Validando eliminacion de menú", EventLevel.Informational);
            try
            {
                Menus = GetAll(obj).ToList();

                if ((from o in Menus
                     where o.Plato.Id_Plato == obj.Plato.Id_Plato && o.Fecha_Dia_Menu == obj.Fecha_Dia_Menu && o.Id_Menu == obj.Id_Menu
                     select o).Any() == true)
                {
                    MenuRepository.Delete(obj);
                    Menus = GetAll(obj).ToList();
                }
                else
                {
                    throw new Exception("No existe un Menu del dia con este plato.");
                }

            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Menu - Error al eliminar menú en: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }

        public void Update(Menu obj)
        {
            //Actualizo los campos del plato
            LoggerManager.Current.Write($"BLL Menu - Validando actualización de menú", EventLevel.Informational);
            try
            {
                Menus = GetAll(obj).ToList();

                if ((from o in Menus
                     where o.Plato.Id_Plato == obj.Plato.Id_Plato && o.Fecha_Dia_Menu == obj.Fecha_Dia_Menu && o.Id_Menu == obj.Id_Menu
                     select o).Any() == true)
                {
                    MenuRepository.Update(obj);
                    Menus = GetAll(obj).ToList();

                }
                else
                {
                    throw new Exception("No existe un Menu del dia con este plato.");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Menu - Error al actualizar menú: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }



        public List<Menu> BuscarMenuxNumeroMenu(Menu obj)
        {
            //Busco un menú a partir del numero menú
            LoggerManager.Current.Write($"BLL Menu - Validando buscar menú por número menú", EventLevel.Informational);
            try
            {
                if ((from o in MenuRepository.GetAll(obj)
                     where o.Numero_Menu == obj.Numero_Menu
                     select o).Any() == true)
                {
                    return (from o in MenuRepository.GetAll(obj)
                            where o.Numero_Menu == obj.Numero_Menu
                            orderby o.Fecha_Dia_Menu descending
                            select o).ToList();
                }
                else
                {
                    //Cuando no coincide el numero de menú lanzo exepcion
                    throw new Exception($"No existre el número de menu {obj.Numero_Menu}");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Menu - Error al buscar menú por número menú: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }


        public List<Menu> BuscarMenuxFechaMenu(Menu obj)
        {
            //Busco un menú a partir de la fecha del menú
            LoggerManager.Current.Write($"BLL Menu - Validando buscar menú por fecha día menú", EventLevel.Informational);
            try
            {
                if ((from o in MenuRepository.GetAll(obj)
                     where o.Fecha_Dia_Menu.Date == obj.Fecha_Dia_Menu.Date
                     select o).Any() == true)
                {
                    return (from o in MenuRepository.GetAll(obj)
                            where o.Fecha_Dia_Menu.Date == obj.Fecha_Dia_Menu.Date
                            orderby o.Fecha_Dia_Menu descending
                            select o).ToList();
                }
                else
                {
                    //Cuando no coincide la fecha con algun menú
                    throw new Exception($"No existe un menú con fecha de menú del día:{obj.Fecha_Dia_Menu}");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Menu - Error al buscar menú por fehca de menú del día: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }



        public List<Menu> BuscarMenuxPlato(Menu obj)
        {
            //Busco un menú a partir del numero plato
            LoggerManager.Current.Write($"BLL Menu - Validando buscar menú por plato", EventLevel.Informational);
            try
            {
                if ((from o in MenuRepository.GetAll(obj)
                     where o.Plato.Id_Plato == obj.Plato.Id_Plato
                     select o).Any() == true)
                {
                    return (from o in MenuRepository.GetAll(obj)
                            where o.Plato.Id_Plato == obj.Plato.Id_Plato
                            orderby o.Fecha_Dia_Menu descending
                            select o).ToList();
                }
                else
                {
                    //Cuando no coincide el numero de plato lanzo exepcion
                    throw new Exception($"No existe un menú con ese plato {obj.Numero_Menu}");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Menu - Error al buscar menú por plato: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }


        //public List<Menu> BuscarMenuxNumeroPlato(Menu obj)
        //{
        //    //Busco un menú a partir del numero plato
        //    LoggerManager.Current.Write($"BLL Menu - Validando buscar menú por número plato", EventLevel.Informational);
        //    try
        //    {
        //        if ((from o in MenuRepository.GetAll(obj)
        //             where o.Plato.Numero_Plato == obj.Plato.Numero_Plato
        //             select o).Any() == true)
        //        {
        //            return (from o in MenuRepository.GetAll(obj)
        //                    where o.Plato.Numero_Plato == obj.Plato.Numero_Plato
        //                    orderby o.Fecha_Dia_Menu descending
        //                    select o).ToList();
        //        }
        //        else
        //        {
        //            //Cuando no coincide el numero de plato lanzo exepcion
        //            throw new Exception($"No existe un menú con ese número de plato {obj.Numero_Menu}");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        LoggerManager.Current.Write($"BLL Menu - Error al buscar menú por número plato: {ex.Message}", EventLevel.Error);
        //        throw new Exception(ex.Message);
        //    }
        //}

        //public List<Menu> BuscarMenuxNombrePlato(Menu obj)
        //{
        //    //Busco un menú a partir de la descirpción del plato
        //    LoggerManager.Current.Write($"BLL Menu - Validando buscar menú por nombre plato", EventLevel.Informational);
        //    try
        //    {
        //        List<Dominio.Menu> menusxdecripcionplato = new List<Dominio.Menu>();
        //        //Busco todos los platos que tengan ese nombre y los agrego a la lista
        //        List<Plato> platos = PlatoBusinessLogic.Current.BuscarPlatoxNombrePlato(obj.Plato);
        //        foreach (var item in platos)
        //        {
        //            //Busco todos los menus asociados a los platos del listado anterior
        //            foreach (var item2 in (BuscarMenuxNumeroPlato(new Dominio.Menu
        //            {
        //                Plato = new Plato { Numero_Plato = item.Numero_Plato }
        //            })))
        //            {
        //                menusxdecripcionplato.Add(item2);
        //            }
        //        }
        //        return (from o in menusxdecripcionplato orderby o.Fecha_Dia_Menu descending select o).ToList();
        //    }
        //    catch (Exception ex)
        //    {
        //        LoggerManager.Current.Write($"BLL Menu - Error al buscar menú por nombre de plato: {ex.Message}", EventLevel.Error);
        //        throw new Exception(ex.Message);
        //    }
        //}


        //public decimal BuscarPrecioMenudelDiaoPrecioVIgentexPlatoyFecha(Menu menu,Plato obj, DateTime fecha)
        //{
        //    //Busco precio de un menú a partir del numero plato y de la fecha
        //    LoggerManager.Current.Write($"BLL Menu - Validando buscar precio menú por número plato y fecha", EventLevel.Informational);
        //    try
        //    {
        //        if ((from o in MenuRepository.GetAll(menu)
        //             where o.Plato.Numero_Plato == obj.Numero_Plato & o.Fecha_Dia_Menu.Date == fecha.Date & o.Estado == true
        //             select o).Any() == true)
        //        {
        //            return (from o in MenuRepository.GetAll(menu)
        //                    where o.Plato.Numero_Plato == obj.Numero_Plato & o.Fecha_Dia_Menu.Date == fecha.Date & o.Estado == true
        //                    select o.Precio_Menu_Plato).FirstOrDefault();
        //        }
        //        else
        //        {
        //            //Cuando no esta como menu del dia
        //            return Plato_PrecioBusinessLogic.Current.BuscarPrecioPorPlatoYFecha(new Plato_Precio { Plato = obj }, fecha).Precio;

        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        LoggerManager.Current.Write($"BLL Menu - Error al buscar precio menú por número plato y fecha: {ex.Message}", EventLevel.Error);
        //        throw new Exception(ex.Message);
        //    }
        //}





        public decimal BuscarPrecioMenudelDiaoPrecioVIgentexPlatoyFecha(Menu menu)
        {
            //Busco precio de un menú a partir del numero plato y de la fecha
            LoggerManager.Current.Write($"BLL Menu - Validando buscar precio menú por número plato y fecha", EventLevel.Informational);
            try
            {
                if ((from o in MenuRepository.GetAll(menu)
                     where o.Plato.Id_Plato == menu.Plato.Id_Plato & o.Fecha_Dia_Menu.Date == menu.Fecha_Dia_Menu.Date & o.Estado == true
                     select o).Any() == true)
                {
                    return (from o in MenuRepository.GetAll(menu)
                            where o.Plato.Id_Plato == menu.Plato.Id_Plato & o.Fecha_Dia_Menu.Date == menu.Fecha_Dia_Menu.Date & o.Estado == true
                            select o.Precio_Menu_Plato).FirstOrDefault();
                }
                else
                {
                    //Cuando no esta como menu del dia
                    return Plato_PrecioBusinessLogic.Current.BuscarPrecioPorPlatoYFecha(new Plato_Precio { Plato = menu.Plato, Id_Empresa=menu.Plato.Id_Empresa, Id_Sucursal=menu.Plato.Id_Sucursal }, menu.Fecha_Dia_Menu).Precio;

                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Menu - Error al buscar precio menú por número plato y fecha: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }

        public List<Plato> BuscarPlatosdeMenuxFechaMenu(Menu obj)
        {
            //Busco paltos del menú a partir de la fecha del menú
            List<Plato> PlatosdelMenudeldia = new List<Plato>();
            LoggerManager.Current.Write($"BLL Menu - Validando buscar platos del menú por fecha día menú", EventLevel.Informational);
            try
            {
                foreach (var item in BuscarMenuxFechaMenu(obj))
                {
                    item.Plato = PlatoBusinessLogic.Current.BuscarPlatoxNumeroPlato(item.Plato);
                    PlatosdelMenudeldia.Add(item.Plato);
                }
                return PlatosdelMenudeldia;
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Menu - Error al buscar platos del menú por fehca de menú del día: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }

    }
}
