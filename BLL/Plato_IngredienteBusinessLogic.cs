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
    public sealed class Plato_IngredienteBusinessLogic : IGenericBusinessLogic<Plato_Ingrediente>
    {
        private readonly static Plato_IngredienteBusinessLogic _instance = new Plato_IngredienteBusinessLogic();

        List<Plato_Ingrediente> platoingredeintes = new List<Plato_Ingrediente>();

        IGenericRepository<Plato_Ingrediente> Plato_IngredienteRepository = Factory.Current.GetPlato_IngredienteRepository();

        public static Plato_IngredienteBusinessLogic Current
        {
            get
            {
                return _instance;
            }
        }

        private Plato_IngredienteBusinessLogic()
        {
            //Implent here the initialization of your singleton
            //platoingredeintes = Plato_IngredienteRepository.GetAll().ToList();
        }

        public void Add(Plato_Ingrediente obj)
        {
            try
            {
                LoggerManager.Current.Write($"BLL Plato-Ingrediente - Validando alta de Plato-Ingrediente", EventLevel.Informational);

                if ((from o in HidratarListPlatoIngrediente(PlatoIngredientexPlato(obj))
                     where o.Ingrediente.Id_Ingrediente == obj.Ingrediente.Id_Ingrediente
                     select o).Any() == true)
                {
                    throw new Exception("Ya existe un ingrediente con ese nombre en este plato.");
                }
                else
                {
                    Plato_IngredienteRepository.Insert(obj);
                }
                platoingredeintes = Plato_IngredienteRepository.GetAll(obj).ToList();
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Plato-Ingrediente - Error al dar de alta Plato-Ingrediente: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }


        }

        public void Remove(Plato_Ingrediente plato_Ingrediente)
        {
            //elimino plato_precio
            try
            {
                LoggerManager.Current.Write($"BLL Plato-Ingrediente - Removiendo Plato-Ingrediente", EventLevel.Informational);
                if ((from o in HidratarListPlatoIngrediente(PlatoIngredientexPlato(plato_Ingrediente))
                     where o.Ingrediente.Id_Ingrediente == plato_Ingrediente.Ingrediente.Id_Ingrediente && o.Id_PI == plato_Ingrediente.Id_PI
                     select o).Any() == true)
                {
                    Plato_IngredienteRepository.Delete(plato_Ingrediente);
                    platoingredeintes = Plato_IngredienteRepository.GetAll(plato_Ingrediente).ToList();
                }
                else
                {
                    throw new Exception("No existe un ingrediente con ese nombre en este plato.");
                }

                
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Plato-Ingrediente - Error al remover Plato-Ingrediente: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }

        public void Update(Plato_Ingrediente plato_Ingrediente)
        {
            try
            {
                //Actualizo Plato-Ingrediente
                LoggerManager.Current.Write($"BLL Plato-Ingrediente - Actualizando Plato-Ingrediente", EventLevel.Informational);
                if ((from o in HidratarListPlatoIngrediente(PlatoIngredientexPlato(plato_Ingrediente))
                     where o.Ingrediente.Id_Ingrediente == plato_Ingrediente.Ingrediente.Id_Ingrediente
                     select o).Any() == true)
                {
                    Plato_IngredienteRepository.Update(plato_Ingrediente);
                    platoingredeintes = Plato_IngredienteRepository.GetAll(plato_Ingrediente).ToList();

                }
                else
                {
                    throw new Exception("No existe un ingrediente con ese nombre en este plato.");
                }
                    
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Plato-Ingrediente - Error al Actualizar Plato-Ingrediente: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }

        public IEnumerable<Plato_Ingrediente> GetAll(Plato_Ingrediente plato_Ingrediente)
        {
            //traigo todo plato_precio
            LoggerManager.Current.Write($"BLL Plato-Ingrediente - Listando todo Plato-Ingrediente", EventLevel.Informational);
            try
            {
                IEnumerable<Plato_Ingrediente> platos_ingredeintes;
                platos_ingredeintes = Plato_IngredienteRepository.GetAll(plato_Ingrediente);
                foreach (var item in platos_ingredeintes)
                {
                    HidratarPlatoIngrediente(item);
                }
                return platos_ingredeintes;
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Precios - Error al Listar Plato-Ingrediente: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }

        }

        public Plato_Ingrediente GetOne(Plato_Ingrediente plato_ingrediente)
        {
            try
            {
                //Busco un plato_precio por id
                LoggerManager.Current.Write($"BLL Plato-Ingrediente - Buscando Plato-Ingrediente por ID", EventLevel.Informational);
                platoingredeintes = Plato_IngredienteRepository.GetAll(plato_ingrediente).ToList();
                if ((from o in platoingredeintes
                     where o.Id_PI == plato_ingrediente.Id_PI
                     select o).Any() == true)
                {
                    return Plato_IngredienteRepository.GetOne(plato_ingrediente);
                }
                    else
                {

                    throw new Exception("No existe un ingrediente con ese id de plato ingrediente");
                }
                
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Plato-Ingrediente - Error al buscar Plato-Ingrediente por ID", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }


        public void HidratarPlatoIngrediente(Plato_Ingrediente obj)
        {
            //Hidrato los platos y los ingredientes del plato_ingredinete
            try
            {
                LoggerManager.Current.Write($"BLL Plato-Ingrediente - Hidratando Plato-Ingrediente", EventLevel.Informational);
                List<Plato> platos = new List<Plato>();
                List<Ingrediente> ingredientes = new List<Ingrediente>();
                platos = PlatoBusinessLogic.Current.GetAll(obj.Plato).ToList();
                ingredientes = IngredienteBusinessLogic.Current.GetAll(obj.Ingrediente).ToList();


                foreach (var item in platos)
                {
                    if (item.Id_Plato == obj.Plato.Id_Plato)
                    {
                        obj.Plato = item;
                    }
                }


                foreach (var item in ingredientes)
                {
                    if (item.Id_Ingrediente == obj.Ingrediente.Id_Ingrediente)
                    {
                        obj.Ingrediente = item;
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Plato-Ingrediente - Error al hidratar Plato-Ingrediente", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }





        public List<Plato_Ingrediente> HidratarListPlatoIngrediente(List<Plato_Ingrediente> obj)
        {
            //Hidrato dentro del listado de platos-ingredientes los platos y los ingredientes de cada uno
            try
            {
                LoggerManager.Current.Write($"BLL Plato-Ingrediente - Hidratando listado de Plato-Ingrediente", EventLevel.Informational);
                //Busco un plato_precio por id
                List<Plato> platos = new List<Plato>();
                List<Ingrediente> ingredientes = new List<Ingrediente>();

                foreach (var item in obj)
                {
                    platos = PlatoBusinessLogic.Current.GetAll(item.Plato).ToList();
                    ingredientes = IngredienteBusinessLogic.Current.GetAll(item.Ingrediente).ToList();
                    foreach (var itemplatos in platos)
                    {
                        if (itemplatos.Id_Plato == item.Plato.Id_Plato)
                        {
                            item.Plato = itemplatos;
                        }
                    }


                    foreach (var itemingredientes in ingredientes)
                    {
                        if (itemingredientes.Id_Ingrediente == item.Ingrediente.Id_Ingrediente)
                        {
                            item.Ingrediente = itemingredientes;
                        }
                    }


                }
                return obj;
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Plato-Ingrediente - Error al hidratar listado de Plato-Ingrediente", EventLevel.Error);
                throw new Exception(ex.Message);
            }


        }





        public List<Plato_Ingrediente> PlatoIngredientexPlato(Plato_Ingrediente plato_Ingrediente)
        {
            //Busco Todos los ingredientes para un plato
            try
            {
                LoggerManager.Current.Write($"BLL Plato-Ingrediente - Buscando Plato Ingredientes por Plato", EventLevel.Informational);


                return  HidratarListPlatoIngrediente( new List<Plato_Ingrediente>(from o in Plato_IngredienteRepository.GetAll(plato_Ingrediente)
                                                   where o.Plato.Id_Plato == plato_Ingrediente.Plato.Id_Plato
                                                   select o));
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Plato-Ingrediente - Error al buscar Plato Ingredientes por Plato", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }


        public List<Plato_Ingrediente> PlatoIngredientexIngrediente(Plato_Ingrediente plato_Ingrediente)
        {
            //Busco Todos los ingredientes para un plato
            try
            {
                LoggerManager.Current.Write($"BLL Plato-Ingrediente - Buscando PlatoIngrediente por Ingrediente", EventLevel.Informational);


                return HidratarListPlatoIngrediente(new List<Plato_Ingrediente>(from o in Plato_IngredienteRepository.GetAll(plato_Ingrediente)
                                                   where o.Ingrediente.Id_Ingrediente == plato_Ingrediente.Ingrediente.Id_Ingrediente
                                                   select o));
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Plato-Ingrediente - Error al buscar PlatoIngrediente por Ingrediente", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }





        //public List<Plato_Ingrediente> PlatoxNombreIngrediente(Ingrediente obj)
        //{
        //    //Busco Platos x nombre de ingrediente que contiene
        //    try
        //    {
        //        LoggerManager.Current.Write($"BLL Plato-Ingrediente - Buscando plato por ingrediente que contiene", EventLevel.Informational);
        //        List<Plato_Ingrediente> ListPlatoxNombreIngrediente = new List<Plato_Ingrediente>();

        //        ListPlatoxNombreIngrediente = Plato_IngredienteRepository.GetAll(new Plato_Ingrediente {Id_Empresa=obj.Id_Empresa,Id_Sucursal=obj.Id_Sucursal }).ToList();

        //        foreach (var item in ListPlatoxNombreIngrediente)
        //        {
        //            HidratarPlatoIngrediente(item);
        //        }

        //        ListPlatoxNombreIngrediente = (from o in ListPlatoxNombreIngrediente
        //                                       where o.Ingrediente.Nombre_Ingrediente.ToUpper().Contains(obj.Nombre_Ingrediente.ToUpper())
        //                                       select o).ToList();

        //        return ListPlatoxNombreIngrediente;
        //    }
        //    catch (Exception ex)
        //    {
        //        LoggerManager.Current.Write($"BLL Plato-Ingrediente - Error al buscar plato por ingrediente que contiene", EventLevel.Error);
        //        throw new Exception(ex.Message);
        //    }


        //}

    }
}
