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
    public sealed class PlatoBusinessLogic : IGenericBusinessLogic<Plato>
    {
        List<Plato> platos = new List<Plato>();
        private readonly static PlatoBusinessLogic _instance = new PlatoBusinessLogic();

        IGenericRepository<Plato> PlatoRepository = Factory.Current.GetPlatoRepository();

        public static PlatoBusinessLogic Current
        {
            get
            {
                return _instance;
            }
        }

        private PlatoBusinessLogic()
        {
            //Implent here the initialization of your singleton
            //platos = PlatoRepository.GetAll().ToList();
        }

        public void Add(Plato obj)
        {
            //Doy de alta un plato
            try
            {
                LoggerManager.Current.Write($"BLL Platos - Validando alta de plato", EventLevel.Informational);
                if (platos.Any(o => o.Nombre_Plato.ToUpper().Equals(obj.Nombre_Plato.ToUpper())))
                {
                    //Ya existe un Plato con ese nombre
                    throw new Exception($"Ya existe un plato con el nombre {obj.Nombre_Plato}");
                }
                else
                {
                    PlatoRepository.Insert(obj);
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Platos: - Error al dar de alta plato: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
            platos = PlatoRepository.GetAll(obj).ToList();
        }

        public void Remove(Plato obj)
        {
            //Remuevo un plato a partir de su ID
            LoggerManager.Current.Write($"BLL Platos - Validando eliminacion de plato", EventLevel.Informational);
            try
            {
                PlatoRepository.Delete(obj);
                platos = PlatoRepository.GetAll(obj).ToList();
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Platos - Error al eliminar plato en: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }

        }

        public void Update(Plato obj)
        {
            //Actualizo los campos del plato
            LoggerManager.Current.Write($"BLL Platos - Validando actualización de plato", EventLevel.Informational);
            try
            {
                PlatoRepository.Update(obj);
                platos = PlatoRepository.GetAll(obj).ToList();
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Platos - Error al actualizar plato: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }

        public IEnumerable<Plato> GetAll(Plato obj)
        {
            //Listo todos los platos en orden descendente
            LoggerManager.Current.Write($"BLL Platos - Validando listar platos", EventLevel.Informational);
            try
            {
                return from o in PlatoRepository.GetAll(obj)
                       orderby o.Numero_Plato descending
                       select o;
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Platos - Error al listar platos: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }

        public Plato GetOne(Plato obj)
        {
            //Busco un plato por su ID
            LoggerManager.Current.Write($"BLL Platos - Validando buscar plato por ID plato", EventLevel.Informational);
            try
            {
                return PlatoRepository.GetOne(obj);
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Platos - Error al buscar plato por ID plato: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }

        public Plato BuscarPlatoxNumeroPlato(Plato obj)
        {
            //Busco un plato a partir del numero plato
            LoggerManager.Current.Write($"BLL Platos - Validando buscar plato por número plato", EventLevel.Informational);
            try
            {
                platos = PlatoRepository.GetAll(obj).ToList();
                if (platos.Any(o => o.Numero_Plato.Equals(obj.Numero_Plato)))
                {
                    obj = platos.FirstOrDefault(o => o.Numero_Plato.Equals(obj.Numero_Plato));
                }
                else
                {
                    //Cuando no coincide el numero de plato lanzo exepcion
                    throw new Exception($"No existe el número de plato {obj.Numero_Plato}");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Platos - Error al buscar plato por número plato: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
            return obj;
        }

        public List<Plato> BuscarPlatoxNombrePlato(Plato obj)
        {
            LoggerManager.Current.Write($"BLL Platos - Validando buscar plato por nombre de plato", EventLevel.Informational);
            List<Plato> PlatosxNombre = new List<Plato>();
            try
            {
                platos = PlatoRepository.GetAll(obj).ToList();
                //Busco platos que en la nombre contengan los valores ingresados por el usuario
                if (platos.Any(o => o.Nombre_Plato.ToUpper().Contains(obj.Nombre_Plato.ToUpper())))
                {
                    PlatosxNombre = (from o in platos
                                     where o.Nombre_Plato.ToUpper().Contains(obj.Nombre_Plato.ToUpper())
                                     select o).ToList();
                }
                else
                {
                    throw new Exception($"Ningun plato contiene en el nombre \"{obj.Nombre_Plato}\"");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Platos - Error al buscar plato por nombre de plato en: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
            return PlatosxNombre;
        }

        public List<Plato> BuscarPlatoxDescripcionPlato(Plato obj)
        {
            List<Plato> PlatosxNombre = new List<Plato>();
            LoggerManager.Current.Write($"BLL Platos - Validando buscar plato por descripción plato", EventLevel.Informational);
            try
            {
                platos = PlatoRepository.GetAll(obj).ToList();
                //Busco platos que en la descirpción contengan los valores ingresados por el usuario
                if (platos.Any(o => o.Descripcion.ToUpper().Contains(obj.Descripcion.ToUpper())))
                {
                    PlatosxNombre = (from o in platos
                                     where o.Descripcion.ToUpper().Contains(obj.Descripcion.ToUpper())
                                     select o).ToList();
                }
                else
                {
                    throw new Exception($"Ningun plato contiene en la descripción \"{obj.Descripcion}\"");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Platos - Error al buscar plato por descipción de plato: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
            return PlatosxNombre;
        }






        public Plato BuscarPlatoxNombrePlatoExacto(Plato obj)
        {
            LoggerManager.Current.Write($"BLL Platos - Validando buscar plato por nombre de plato exacto", EventLevel.Informational);
            try
            {
                platos = PlatoRepository.GetAll(obj).ToList();
                //Busco platos que en la nombre contengan los valores ingresados por el usuario
                if (platos.Any(o => o.Nombre_Plato.ToUpper().Equals(obj.Nombre_Plato.ToUpper())))
                {
                    return (from o in platos
                            where o.Nombre_Plato.ToUpper().Equals(obj.Nombre_Plato.ToUpper())
                            select o).FirstOrDefault();
                }
                else
                {
                    throw new Exception($"Ningun plato contiene en el nombre \"{obj.Nombre_Plato}\"");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Platos - Error al buscar plato por nombre de plato exacto en: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }
    }
}
