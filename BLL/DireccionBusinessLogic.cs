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
using Servicios.Services.Extensions;

namespace BLL
{
    public sealed class DireccionBusinessLogic : IGenericBusinessLogic<Direccion>
    {
        List<Direccion> direcciones = new List<Direccion>();
        private readonly static DireccionBusinessLogic _instance = new DireccionBusinessLogic();

        IGenericRepository<Direccion> DireccionesRepository = Factory.Current.GetDireccionesRepository();

        public static DireccionBusinessLogic Current
        {
            get
            {
                return _instance;
            }
        }

        private DireccionBusinessLogic()
        {
            //Implent here the initialization of your singleton
            //direcciones = DireccionesRepository.GetAll().ToList();
        }

        public void Add(Direccion obj)
        {

            //Doy de alta una dirección
            try
            {
                bool estado = true;
                LoggerManager.Current.Write($"BLL Direcciones - Validando alta de dirección", EventLevel.Informational);
                if (obj.Cliente != null)
                {
                    //Valido si el cliente ya tiene un dirección cargado con esos datos
                    if (direcciones.Any(o => o.Cliente.Numero_Cliente == obj.Cliente.Numero_Cliente && o.Nombre_Calle.ToUpper().Equals(obj.Nombre_Calle.ToUpper()) && o.Altura == obj.Altura && o.Piso == obj.Piso && o.Localidad == obj.Localidad))
                    {
                        //Ya existe un dirección con esos datos
                        estado = false;
                        throw new Exception($"El cliente ya tiene una dirección: {obj.Nombre_Calle} {obj.Altura}".Traducir());
                    }
                    else if (direcciones.Any(o => o.Cliente.Numero_Cliente == obj.Cliente.Numero_Cliente && o.Tipo_Direccion.ToUpper().Equals(obj.Tipo_Direccion.ToUpper())))
                    {
                        estado = false;
                        //Ya existe una dirección con ese nombre de dirección
                        throw new Exception($"El cliente ya tiene un dirección con el nombre:: {obj.Tipo_Direccion}".Traducir());
                    }
                }
                if (estado == true && obj.Cliente != null)
                {
                    DireccionesRepository.Insert(obj);
                }
                else
                {
                    throw new Exception($"Cliente invalido");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Direcciones: - Error al dar de alta dirección: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
            direcciones = DireccionesRepository.GetAll(obj).ToList();

        }

        public void Remove(Direccion obj)
        {
            //Remuevo un direccion a partir de su ID
            LoggerManager.Current.Write($"BLL Direcciones - Validando eliminacion de dirección", EventLevel.Informational);
            try
            {
                DireccionesRepository.Delete(obj);
                direcciones = DireccionesRepository.GetAll(obj).ToList();
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Direcciones - Error al eliminar dirección en: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }

        }

        public void Update(Direccion obj)
        {
            //Actualizo una dirección
            try
            {
                direcciones = DireccionesRepository.GetAll(obj).ToList();
                bool estado = true;
                LoggerManager.Current.Write($"BLL Direcciones - Validando actualización de dirección", EventLevel.Informational);
                if (obj.Cliente != null)
                {
                    //Valido si el cliente ya tiene un dirección cargado con esos datos distinta a la actual
                    if (direcciones.Any(o => o.Cliente.Numero_Cliente == obj.Cliente.Numero_Cliente && o.Nombre_Calle.ToUpper().Equals(obj.Nombre_Calle.ToUpper()) && o.Altura == obj.Altura && o.Piso == obj.Piso && o.Localidad == obj.Localidad && o.Id_Direccion != obj.Id_Direccion))
                    {
                        //Ya existe un dirección con esos datos distinta a la actual
                        estado = false;
                        throw new Exception($"El cliente ya tiene una dirección:  {obj.Nombre_Calle} {obj.Altura}".Traducir());
                    }
                    else if ((direcciones.Any(o => o.Cliente.Numero_Cliente == obj.Cliente.Numero_Cliente && o.Tipo_Direccion.ToUpper().Equals(obj.Tipo_Direccion.ToUpper()) && o.Id_Direccion != obj.Id_Direccion)))
                    {
                        estado = false;
                        //Ya existe una dirección con ese nombre de dirección distinta a la actual
                        throw new Exception($"El cliente ya tiene un dirección con el nombre: {obj.Tipo_Direccion}".Traducir());
                    }
                }
                if (estado == true)
                {
                    DireccionesRepository.Update(obj);
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Direcciones: - Error al actualizar dirección: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
            direcciones = DireccionesRepository.GetAll(obj).ToList();
        }

        public IEnumerable<Direccion> GetAll(Direccion obj)
        { 
            //Listo todas las direcciones en orden descendente
            LoggerManager.Current.Write($"BLL Direcciones - Validando listar direcciones", EventLevel.Informational);
            try
            {
                direcciones = DireccionesRepository.GetAll(obj).ToList();
                return from o in DireccionesRepository.GetAll(obj)
                       orderby o.Numero_Direccion descending
                       select o;
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Direcciones - Error al listar direcciones: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }

        public Direccion GetOne(Direccion obj)
        {
            //Busco un dirección por su ID
            LoggerManager.Current.Write($"BLL Direcciones - Validando buscar dirección por ID dirección", EventLevel.Informational);
            try
            {
                direcciones = DireccionesRepository.GetAll(obj).ToList();
                return DireccionesRepository.GetOne(obj);
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Direcciones - Error al buscar dirección por ID dirección: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }


        //public Direcciones BuscarDireccionxNumeroDireccion(Direcciones obj)
        //{
        //    if (direcciones.Any(o => o.Numero_Direccion.Equals(obj.Numero_Direccion)))
        //    {
        //        obj = direcciones.FirstOrDefault(o => o.Numero_Direccion.Equals(obj.Numero_Direccion));
        //    }
        //    else
        //    {

        //    }
        //    return obj;

        //}

        public Direccion BuscarDireccionxNumeroDireccion(Direccion obj)
        {
            //Busco un direción a partir del numero direccón
            LoggerManager.Current.Write($"BLL Direcciones - Validando buscar dirección por número dirección", EventLevel.Informational);
            try
            {
                direcciones = DireccionesRepository.GetAll(obj).ToList();
                if (direcciones.Any(o => o.Numero_Direccion.Equals(obj.Numero_Direccion)))
                {
                    obj = direcciones.FirstOrDefault(o => o.Numero_Direccion.Equals(obj.Numero_Direccion));
                }
                else
                {
                    //Cuando no coincide el numero de direccón lanzo exepcion
                    throw new Exception($"No existe dirección con el número {obj.Numero_Direccion}".Traducir());
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Direcciones - Error al buscar dirección por número dirección: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
            return obj;
        }

        public List<Direccion> BuscarDireccionxNumeroCliente(Direccion obj)
        {
            //Busco un cliente a partir del numero cliente
            LoggerManager.Current.Write($"BLL Direcciones - Validando buscar Direccion por número cliente", EventLevel.Informational);
            try
            {
                direcciones = DireccionesRepository.GetAll(obj).ToList();
                //Retorno todas las direcciones del cliente
                return (from o in direcciones where o.Cliente.Numero_Cliente.Equals(obj.Cliente.Numero_Cliente) select o).ToList();
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Direcciones - Error al buscar Direccion por número cliente: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }
    }
}
