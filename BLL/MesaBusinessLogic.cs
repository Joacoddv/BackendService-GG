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
    public sealed class MesaBusinessLogic : IGenericBusinessLogic<Mesa>
    {
        private List<Mesa> mesas = new List<Mesa>();

        private readonly static MesaBusinessLogic _instance = new MesaBusinessLogic();

        IGenericRepository<Mesa> MesaRepository = Factory.Current.GetMesaRepository();

        public static MesaBusinessLogic Current
        {
            get
            {
                return _instance;
            }
        }

        private MesaBusinessLogic()
        {
            //Implent here the initialization of your singleton}
            
        }
        public void Add(Mesa obj)
        {
            //Doy de alta una Empresa
            try
            {
                mesas = MesaRepository.GetAll(obj).ToList();
                if (mesas.Any(o => o.Id_Mesa.Equals(obj.Id_Mesa)))
                {
                    throw new Exception($"Ya existe la mesa que se desea crear");
                }
                else
                {
                    MesaRepository.Insert(obj);
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Mesa - Error al dar de alta mesa: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
            mesas = MesaRepository.GetAll(obj).ToList();


        }

        public IEnumerable<Mesa> GetAll(Mesa mesa)
        {
            //Listo todos las empresa en orden descendente
            LoggerManager.Current.Write($"BLL Mesa - Validando listar mesas", EventLevel.Informational);
            try
            {
                return from o in MesaRepository.GetAll(mesa)
                       orderby o.Numero_Mesa descending
                       select o;
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Mesa - Error al listar mesas: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }


        }

        public Mesa GetOne(Mesa mesa)
        {
            //Busco una empresa por su ID
            LoggerManager.Current.Write($"BLL Mesa - Validando buscar empresa por ID mesa", EventLevel.Informational);
            try
            {
                return MesaRepository.GetOne(mesa);
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Mesa - Error al buscar empresa por ID mesa: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }

        public void Remove(Mesa mesa)
        {
            //Desactivo empresa
            try
            {
                LoggerManager.Current.Write($"BLL Mesa - Validando desactivación de mesa", EventLevel.Informational);
                mesas = MesaRepository.GetAll(mesa).ToList();
                if (mesas.Any(o => o.Id_Mesa.Equals(mesa.Id_Mesa)))
                {
                    MesaRepository.Delete(mesa);
                }
                else
                {
                    throw new Exception($"No existe la mesa que se desea eliminar");
                }
                mesas = MesaRepository.GetAll(mesa).ToList();
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Mesa: - Error al eliminar mesa: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }



        }

        public void Update(Mesa mesa)
        {
            //Actualizo empresa
            try
            {
                LoggerManager.Current.Write($"BLL Mesa - Validando alta de mesa", EventLevel.Informational);

                mesas = MesaRepository.GetAll(mesa).ToList();
                if (mesas.Any(o => o.Id_Mesa.Equals(mesa.Id_Mesa)))
                {
                    MesaRepository.Update(mesa);
                }
                else
                {
                    throw new Exception($"No existe la mesa que se desea actualizar");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Mesa: - Error al actualizar mesa: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
            mesas = MesaRepository.GetAll(mesa).ToList();


        }

        public Mesa BuscarMesaxNumeroMesaExacto(Mesa mesa)
        {
            //Busco un empresa a partir del numero empresa
            LoggerManager.Current.Write($"BLL Mesa - Validando buscar empresa por número mesa excato", EventLevel.Informational);
            try
            {
                mesas = MesaRepository.GetAll(mesa).ToList();
                if (mesas.Any(o => o.Numero_Mesa.Equals(mesa.Numero_Mesa)))
                {
                    mesa = mesas.FirstOrDefault(o => o.Numero_Mesa.Equals(mesa.Numero_Mesa));
                }
                else
                {
                    //Cuando no coincide el numero de empresa lanzo exepcion
                    throw new Exception($"No existe mesa con el número {mesa.Numero_Mesa}");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Mesa - Error al buscar mesa por número mesa exacto: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
            return mesa;
        }

        public List<Mesa> BuscarMesaxNumeroMesa(Mesa mesa)
        {
            //Busco un empresa a partir del Numero empresa
            LoggerManager.Current.Write($"BLL Mesa - Validando buscar mesa por número mesa", EventLevel.Informational);
            mesas = MesaRepository.GetAll(mesa).ToList();
            List<Mesa> mesassxnumero = new List<Mesa>();
            try
            {
                //Busco empresa que en tenga en el número de empresa  el valor ingresado por el usuario
                if (mesas.Any(o => o.Numero_Mesa.ToString().Trim().ToUpper().Contains(mesa.Numero_Mesa.ToString().ToUpper().Trim())))
                {
                    mesassxnumero = (from o in mesas
                                              where o.Numero_Mesa.ToString().Trim().ToUpper().Contains(mesa.Numero_Mesa.ToString().ToUpper().Trim())
                                               select o).ToList();
                }
                else
                {
                    throw new Exception($"Ninguna mesa contiene en su número: \"{mesa.Numero_Mesa}\"");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Mesa - Error al buscar mesa por número de mesa: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
            return mesassxnumero;
        }


        public List<Mesa> BuscarMesaxCapacidad(Mesa mesa)
        {
            //Busco un empresa a partir del Numero empresa
            LoggerManager.Current.Write($"BLL Mesa - Validando buscar mesa por capacidad demesa", EventLevel.Informational);
            mesas = MesaRepository.GetAll(mesa).ToList();
            List<Mesa> mesassxcapacidad = new List<Mesa>();
            try
            {
                //Busco empresa que en tenga en el número de empresa  el valor ingresado por el usuario
                if (mesas.Any(o => o.Cantidad >= mesa.Cantidad))
                {
                    mesassxcapacidad = (from o in mesas
                                     where o.Cantidad >= mesa.Cantidad
                                     select o).ToList();
                }
                else
                {
                    throw new Exception($"Ninguna mesa tiene capacidad para: \"{mesa.Cantidad}\"");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Mesa - Error al buscar mesa por capacidad: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
            return mesassxcapacidad;
        }
    }
}
