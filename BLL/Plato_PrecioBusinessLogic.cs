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
    public sealed class Plato_PrecioBusinessLogic : IGenericBusinessLogic<Plato_Precio>
    {
        private List<Plato_Precio> plato_precios = new List<Plato_Precio>();

        private readonly static Plato_PrecioBusinessLogic _instance = new Plato_PrecioBusinessLogic();

        IGenericRepository<Plato_Precio> Plato_PrecioRepository = Factory.Current.GetPlato_PrecioRepository();

        public static Plato_PrecioBusinessLogic Current
        {
            get
            {
                return _instance;
            }
        }

        private Plato_PrecioBusinessLogic()
        {
            //Implent here the initialization of your singleton
            //plato_precios = Plato_PrecioRepository.GetAll().ToList();
        }
        //agrego plato precio
        public void Add(Plato_Precio obj)
        {
            try
            {
                LoggerManager.Current.Write($"BLL Precio - Validando alta de precio", EventLevel.Informational);
                Plato_Precio UltimoPlatoPrecio = new Plato_Precio();

                UltimoPlatoPrecio = (from o in Plato_PrecioRepository.GetAll(obj)
                                     where o.Plato.Id_Plato == obj.Plato.Id_Plato
                                     orderby o.Fecha_Hasta descending
                                     select o).FirstOrDefault();


                //Si la fecha desde es mayor a la fecha hasta actual o si no existe un precio
                if (UltimoPlatoPrecio == null)
                {
                    LoggerManager.Current.Write($"BLL Precio - Realizando alta de precio", EventLevel.Informational);
                    Plato_PrecioRepository.Insert(obj);
                }
                else if (UltimoPlatoPrecio.Fecha_Hasta.Date < obj.Fecha_Desde.Date)
                {
                    LoggerManager.Current.Write($"BLL Precio - Realizando alta de precio", EventLevel.Informational);
                    Plato_PrecioRepository.Insert(obj);
                }
                else
                {
                    throw new Exception("Ya hay un precio establecido dentro del rango de fechas seleccionado");
                }
                plato_precios = Plato_PrecioRepository.GetAll(obj).ToList();
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Precios - Error al dar de alta precio: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }

        //elimino plato_precio
        public void Remove(Plato_Precio obj)
        {
            try
            {
                LoggerManager.Current.Write($"BLL Precio - Removiendo precio", EventLevel.Informational);
                plato_precios = Plato_PrecioRepository.GetAll(obj).ToList();
                if ((from o in plato_precios
                     where o.Plato.Id_Plato == obj.Plato.Id_Plato && o.Id_Plato_Precio == obj.Id_Plato_Precio
                     select o).Any() == true)
                {
                    Plato_PrecioRepository.Delete(obj);
                plato_precios = Plato_PrecioRepository.GetAll(obj).ToList();
                }
                else
                {
                    throw new Exception("No existe un Precio en esa fecha para ese plato.");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Precios - Error al remover precio: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }

        }

        //update plato_precio
        public void Update(Plato_Precio obj)
        {
            try
            {
                LoggerManager.Current.Write($"BLL Precio - Actualizando precio", EventLevel.Informational);

                plato_precios = Plato_PrecioRepository.GetAll(obj).ToList();

                if ((from o in plato_precios
                     where o.Plato.Id_Plato == obj.Plato.Id_Plato && o.Id_Plato_Precio == obj.Id_Plato_Precio
                     select o).Any() == true)
                {
                    Plato_PrecioRepository.Update(obj);
                    plato_precios = Plato_PrecioRepository.GetAll(obj).ToList();
                }
                else
                {
                    throw new Exception("No existe un Precio en esa fecha para ese plato.");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Precios - Error al Actualizar precio: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }

        //traigo todo plato_precio
        public IEnumerable<Plato_Precio> GetAll(Plato_Precio obj)
        {
            try
            {
                LoggerManager.Current.Write($"BLL Precio - Listando precios", EventLevel.Informational);
                return from o in Plato_PrecioRepository.GetAll(obj)
                       orderby o.Numero_Plato_Precio descending
                       select o;
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Precios - Error al Listar precios: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }

        public Plato_Precio GetOne(Plato_Precio obj)
        {
            try
            {
                LoggerManager.Current.Write($"BLL Precio - Buscando precio por ID", EventLevel.Informational);
                //Busco un plato_precio por id
                return Plato_PrecioRepository.GetOne(obj);
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Precio - Error al buscar precio por ID", EventLevel.Error);
                throw new Exception(ex.Message);
            }

        }


        //Busco Precio de un plato a partir del precioplato y la fecha
        public Plato_Precio BuscarPrecioPorPlatoYFecha(Plato_Precio obj, DateTime fechaprecio)
        {
            try
            {
                LoggerManager.Current.Write($"BLL Precio - Buscando precio por plato y fecha", EventLevel.Informational);
                plato_precios = Plato_PrecioRepository.GetAll(obj).ToList();
                if (plato_precios.Any(o => o.Plato.Id_Plato.Equals(obj.Plato.Id_Plato) & o.Fecha_Desde.Date <= fechaprecio.Date & o.Fecha_Hasta.Date >= fechaprecio.Date))
                {
                    obj = plato_precios.FirstOrDefault(o => o.Plato.Id_Plato.Equals(obj.Plato.Id_Plato) & o.Fecha_Desde.Date <= fechaprecio.Date & o.Fecha_Hasta.Date >= fechaprecio.Date);
                }
                else
                {
                    //Excepcion si no encuentro el precio para ese plato en esa fecha
                    throw new Exception($"No se encuentra un precio para el plato en la fecha {fechaprecio.ToString("MM/dd/yyyy")}");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Precio - Error al buscar precio por plato y fecha", EventLevel.Error);
                throw new Exception(ex.Message);
            }
            return obj;
        }



        public List<Plato_Precio> BuscarPlatoPrecioXPlato(Plato_Precio plato_precio)
        {
            try
            {
                LoggerManager.Current.Write($"BLL Precio - Buscando precio por plato", EventLevel.Informational);
                plato_precios = Plato_PrecioRepository.GetAll(plato_precio).ToList();
                if (plato_precios.Any(o => o.Plato.Id_Plato.Equals(plato_precio.Plato.Id_Plato)))
                {
                    return (from o in Plato_PrecioRepository.GetAll(plato_precio)
                            where o.Plato.Id_Plato == plato_precio.Plato.Id_Plato
                            select o).ToList();
                }
                else
                {
                    //Excepcion si no encuentro el precio para ese plato en esa fecha
                    throw new Exception($"No se encuentra un precio para el plato");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Precio - Error al buscar precio por plato", EventLevel.Error);
                throw new Exception(ex.Message);
            }

        }


        ////Busco Precio de un plato a partir del precioplato y la fecha
        //public Plato_Precio BuscarPrecioPorPlatoYFecha(Plato_Precio obj, DateTime fechaprecio)
        //{
        //    try
        //    {
        //        LoggerManager.Current.Write($"BLL Precio - Buscando precio por plato y fecha", EventLevel.Informational);
        //        if (plato_precios.Any(o => o.Plato.Numero_Plato.Equals(obj.Plato.Numero_Plato) & o.Fecha_Desde.Date <= fechaprecio.Date & o.Fecha_Hasta.Date >= fechaprecio.Date))
        //        {
        //            obj = plato_precios.FirstOrDefault(o => o.Plato.Numero_Plato.Equals(obj.Plato.Numero_Plato) & o.Fecha_Desde.Date <= fechaprecio.Date & o.Fecha_Hasta.Date >= fechaprecio.Date);
        //        }
        //        else
        //        {
        //            //Excepcion si no encuentro el precio para ese plato en esa fecha
        //            throw new Exception($"No se encuentra un precio para el plato {obj.Plato.Nombre_Plato} en la fecha {fechaprecio.ToString("MM/dd/yyyy")}");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        LoggerManager.Current.Write($"BLL Precio - Error al buscar precio por plato y fecha", EventLevel.Error);
        //        throw new Exception(ex.Message);
        //    }
        //    return obj;
        //}



        //public List<Plato_Precio> BuscarPlatoPrecioXPlato(Plato_Precio plato_precio, Plato plato)
        //{
        //    try
        //    {
        //        LoggerManager.Current.Write($"BLL Precio - Buscando precio por plato", EventLevel.Informational);
        //        if (plato_precios.Any(o => o.Plato.Numero_Plato.Equals(plato.Numero_Plato)))
        //        {
        //            return (from o in Plato_PrecioRepository.GetAll(plato_precio)
        //                    where o.Plato.Numero_Plato == plato.Numero_Plato
        //                    select o).ToList();
        //        }
        //        else
        //        {
        //            //Excepcion si no encuentro el precio para ese plato en esa fecha
        //            throw new Exception($"No se encuentra un precio para el plato {plato.Nombre_Plato}");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        LoggerManager.Current.Write($"BLL Precio - Error al buscar precio por plato", EventLevel.Error);
        //        throw new Exception(ex.Message);
        //    }

        //}
    }
}
