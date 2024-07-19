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
    public sealed class IngredienteBusinessLogic : IGenericBusinessLogic<Ingrediente>
    {
        private readonly static IngredienteBusinessLogic _instance = new IngredienteBusinessLogic();

        IGenericRepository<Ingrediente> IngredienteRepository = Factory.Current.GetIngredienteRepository();

        List<Ingrediente> ingredientes = new List<Ingrediente>();

        public static IngredienteBusinessLogic Current
        {
            get
            {
                return _instance;
            }
        }

        public IngredienteBusinessLogic()
        {
            //Implent here the initialization of your singleton
             //ingredientes = IngredienteRepository.GetAll().ToList();
        }

        public void Add(Ingrediente obj)
        {
            //Doy de alta un ingrediente
            try
            {
                LoggerManager.Current.Write($"Validando alta de ingrediente en BLL Ingrediente", EventLevel.Informational);
                if (ingredientes.Any(o => o.Nombre_Ingrediente.ToUpper().Equals(obj.Nombre_Ingrediente.ToUpper())))
                {
                    //Ya existe un Ingrediente con ese nombre
                    throw new Exception($"Ya existe un ingrediente con el nombre {obj.Nombre_Ingrediente}");
                }
                else
                {
                    //obj.Id_Ingredientes = Guid.NewGuid();
                    IngredienteRepository.Insert(obj);
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"Error al dar de alta ingrediente en BLL Ingrediente: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
            ingredientes = IngredienteRepository.GetAll(obj).ToList();
        }

        public void Remove(Ingrediente obj)
        {
            //Remuevo un ingrediente a partir de su ID
            LoggerManager.Current.Write($"Validando eliminacion de ingrediente en BLL Ingrediente", EventLevel.Informational);
            try
            {
                ingredientes = IngredienteRepository.GetAll(obj).ToList();
                IngredienteRepository.Delete(obj);
                ingredientes = IngredienteRepository.GetAll(obj).ToList();
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"Error al eliminar ingrediente en BLL Ingrediente: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }

        public void Update(Ingrediente obj)
        {
            //Actualizo los campos del ingrediente
            LoggerManager.Current.Write($"Validando actualización de ingrediente en BLL Ingrediente", EventLevel.Informational);
            try
            {
                ingredientes = IngredienteRepository.GetAll(obj).ToList();
                IngredienteRepository.Update(obj);
                ingredientes = IngredienteRepository.GetAll(obj).ToList();
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"Error al actualizar ingrediente en BLL Ingrediente: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }

        }

        public IEnumerable<Ingrediente> GetAll(Ingrediente obj)
        {
            //Listo todos los ingrediente en orden descendente
            LoggerManager.Current.Write($"Validando listar ingrediente en BLL Ingrediente", EventLevel.Informational);
            try
            {
                return from o in IngredienteRepository.GetAll(obj)
                       orderby o.Numero_ingrediente descending
                       select o;
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"Error al listar ingrediente en BLL Ingrediente: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }

        }

        public Ingrediente GetOne(Ingrediente obj)
        {
            //Busco un ingrediente por su ID
            LoggerManager.Current.Write($"Validando buscar ingrediente por ID ingrediente en BLL Ingrediente", EventLevel.Informational);
            try
            {
                ingredientes = IngredienteRepository.GetAll(obj).ToList();
                return IngredienteRepository.GetOne(obj);
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"Error al buscar ingrediente por ID ingrediente en BLL Ingrediente: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }

        public Ingrediente BuscarIngredientexNumeroIngrediente(Ingrediente obj)
        {
            LoggerManager.Current.Write($"Validando buscar ingrediente por número ingrediente en BLL Ingredientes", EventLevel.Informational);
            try
            {
                ingredientes = IngredienteRepository.GetAll(obj).ToList();
                //Busco ingredientes que el numero de ingrediente sea igual al valor ingresado por el usuario
                if (ingredientes.Any(o => o.Numero_ingrediente.Equals(obj.Numero_ingrediente)))
                {
                    obj = ingredientes.FirstOrDefault(o => o.Numero_ingrediente.Equals(obj.Numero_ingrediente));
                }
                else
                {
                    throw new Exception($"Ningun ingrediente tiene el número \"{ obj.Numero_ingrediente}\"");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"Error al buscar ingrediente por número de ingrediente en BLL Ingrediente: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
            return obj;
        }


        public List<Ingrediente> BuscarIngredientexNombreIngrediente(Ingrediente obj)
        {
            List<Ingrediente> IngredientexNombre = new List<Ingrediente>();
            LoggerManager.Current.Write($"Validando buscar ingrediente por nombre ingrediente en BLL Ingredientes", EventLevel.Informational);
            try
            {
                ingredientes = IngredienteRepository.GetAll(obj).ToList();
                //Busco ingredientes que en el nombre contengan los valores ingresados por el usuario
                if (ingredientes.Any(o => o.Nombre_Ingrediente.ToUpper().Contains(obj.Nombre_Ingrediente.ToUpper())))
                {
                    IngredientexNombre = (from o in ingredientes
                                          where o.Nombre_Ingrediente.ToUpper().Contains(obj.Nombre_Ingrediente.ToUpper())
                                          select o).ToList();
                }
                else
                {
                    throw new Exception($"Ningun ingrediente contiene en su nombre \"{ obj.Nombre_Ingrediente}\"");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"Error al buscar ingrediente por nombre de ingrediente en BLL Ingrediente: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
            return IngredientexNombre;
        }


        public List<Ingrediente> BuscarIngredientexDescrpicionIngrediente(Ingrediente obj)
        {
            List<Ingrediente> IngredientexDescripcion = new List<Ingrediente>();
            LoggerManager.Current.Write($"Validando buscar ingrediente por descripción ingrediente en BLL Ingredientes", EventLevel.Informational);
            try
            {
                ingredientes = IngredienteRepository.GetAll(obj).ToList();
                //Busco ingredientes que en la descripción contengan los valores ingresados por el usuario
                if (ingredientes.Any(o => o.Descripcion.ToUpper().Contains(obj.Descripcion.ToUpper())))
                {
                    IngredientexDescripcion = (from o in ingredientes
                                               where o.Descripcion.ToUpper().Contains(obj.Descripcion.ToUpper())
                                               select o).ToList();
                }
                else
                {
                    throw new Exception($"Ningun ingrediente contiene en su descripción \"{ obj.Descripcion}\"");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"Error al buscar ingrediente por descripción de ingrediente en BLL Ingrediente: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
            return IngredientexDescripcion;
        }


        public List<Ingrediente> BuscarIngredientexMedidaIngrediente(Ingrediente obj)
        {
            List<Ingrediente> IngredientexMedida = new List<Ingrediente>();
            LoggerManager.Current.Write($"Validando buscar ingrediente por medida ingrediente en BLL Ingredientes", EventLevel.Informational);
            try
            {
                ingredientes = IngredienteRepository.GetAll(obj).ToList();
                //Busco ingredientes que en la medida contengan los valores ingresados por el usuario
                if (ingredientes.Any(o => o.Medida.ToUpper().Contains(obj.Medida.ToUpper())))
                {
                    IngredientexMedida = (from o in ingredientes
                                          where o.Medida.ToUpper().Contains(obj.Medida.ToUpper())
                                          select o).ToList();
                }
                else
                {
                    throw new Exception($"Ningun ingrediente utiliza la Medida \"{ obj.Medida}\"");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"Error al buscar ingrediente por medida de ingrediente en BLL Ingrediente: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
            return IngredientexMedida;
        }




        public Ingrediente BuscarUnIngredientexNombreIngredienteExacto(Ingrediente obj)
        {
            LoggerManager.Current.Write($"Validando buscar 1 ingrediente por nombre ingrediente exacto en BLL Ingredientes", EventLevel.Informational);
            try
            {
                ingredientes = IngredienteRepository.GetAll(obj).ToList();
                //Busco ingredientes que en el nombre contengan los valores ingresados por el usuario
                if (ingredientes.Any(o => o.Nombre_Ingrediente.ToUpper().Equals(obj.Nombre_Ingrediente.ToUpper())))
                {
                    return (from o in ingredientes
                            where o.Nombre_Ingrediente.ToUpper().Equals(obj.Nombre_Ingrediente.ToUpper())
                            select o).FirstOrDefault();
                }
                else
                {
                    throw new Exception($"Ningun ingrediente tiene de nombre \"{ obj.Nombre_Ingrediente}\"");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"Error al buscar 1 ingrediente por nombre excato de ingrediente en BLL Ingrediente: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }





    }
}
