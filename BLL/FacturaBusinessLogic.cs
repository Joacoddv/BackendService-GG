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
    public sealed class FacturaBusinessLogic : IGenericBusinessLogic<Factura>
    {
        private readonly static FacturaBusinessLogic _instance = new FacturaBusinessLogic();

        IGenericRepository<Factura> FacturaRepository = Factory.Current.GetFacturaRepository();

        private List<Factura> facturas = new List<Factura>();

        public static FacturaBusinessLogic Current
        {
            get
            {
                return _instance;
            }
        }

        private FacturaBusinessLogic()
        {
            //Implent here the initialization of your singleton
            //facturas = GetAll().ToList();
        }

        public void Add(Factura obj)
        {
            try
            {
                LoggerManager.Current.Write($"BLL Factura - Validando crear factura", EventLevel.Informational);
                FacturaRepository.Insert(obj);
                facturas = GetAll(obj).ToList();
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Factura: - Error al crear factura: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }

        public void Remove(Factura obj)
        {
            try
            {
                LoggerManager.Current.Write($"BLL Factura - Validando Eliminar Factura", EventLevel.Informational);
                if (GetOne(obj) != null)
                {
                    FacturaRepository.Delete(obj);
                    facturas = GetAll(obj).ToList();
                    LoggerManager.Current.Write($"BLL Factura - Elimnando Factura", EventLevel.Informational);
                }
                else
                {
                    throw new Exception("No existe la factura que se desea eliminar");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Factura: - Error al eliminar factura: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }

        public void Update(Factura obj)
        {
            try
            {
                LoggerManager.Current.Write($"BLL Factura - Validando actualizar factura", EventLevel.Informational);
                if (GetOne(obj) != null)
                {
                    //Valido si el nuevo estado es un estado valido
                    if (ValidarEstadosPosibles(GetOne(obj)).Any(o => o.Equals(obj.Estado)))
                    {
                        //actualizo estado factura
                        FacturaRepository.Update(obj);
                        facturas = GetAll(obj).ToList();
                        //Actualizo estado de los pedidos de la factura
                        ActualizarEstadopedidosFactura(obj);
                    }
                    else
                    {
                        if (ValidarEstadosPosibles(GetOne(obj)).Count > 1)
                        {
                            throw new Exception($"El estado de factura seleccionado no es un estado válido. Los válidos son: {string.Join(", ", ValidarEstadosPosibles(obj))}");
                        }
                        else
                        {
                            throw new Exception("La factura se encuentra en un estado terminal que no se puede actualizar");
                        }
                    }
                }
                else
                {
                    throw new Exception("No existe la factura que se desea actualizar");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Factura: - Error al actualizar factura: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }

        public IEnumerable<Factura> GetAll(Factura obj)
        {
            try
            {
                LoggerManager.Current.Write($"BLL Factura - Validando buscar facturas", EventLevel.Informational);
                return from o in FacturaRepository.GetAll(obj)
                       orderby o.Numero_Factura descending
                       select o;
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Factura: - Error al buscar facturas: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }

        public Factura GetOne(Factura obj)
        {
            try
            {
                LoggerManager.Current.Write($"BLL Factura - Validando buscar una factura por id", EventLevel.Informational);
                return FacturaRepository.GetOne(obj);
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Factura: - Error al buscar una factura por id: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }



        public void ActualizarEstadopedidosFactura(Factura factura)
        {
            LoggerManager.Current.Write($"BLL Factura - Actualizando estado factura-pedido en base al estado de la factura", EventLevel.Informational);
            try
            {
                //Valido el estado de la factura y actualizo estado de pedidos
                if (factura.Estado == EEstadoFactura.Pagada)
                {
                    Factura_PedidoBusinessLogic.Current.EstadoPagadoPedidosdelaFactura(factura);
                }
                else if (factura.Estado == EEstadoFactura.Cancelada)
                {
                    Factura_PedidoBusinessLogic.Current.EstadoNOFacturadoPedidosdelaFactura(factura);
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Factura - Error al actualizar estado factura-pedido en base al estado de la factura: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }







        public List<EEstadoFactura> ValidarEstadosPosibles(Factura obj)
        {
            List<EEstadoFactura> ListaPosiblesEstados = new List<EEstadoFactura>();
            switch (obj.Estado)
            {
                case EEstadoFactura.Creada:
                    ListaPosiblesEstados.Add(EEstadoFactura.Pagada);
                    ListaPosiblesEstados.Add(EEstadoFactura.Cancelada);
                    break;
                case EEstadoFactura.Pagada:
                    break;
                case EEstadoFactura.Cancelada:
                    break;
                default:
                    break;
            }
            return ListaPosiblesEstados;
        }


        public Factura BuscarFacturaxNumeroFacturaExacto(Factura obj)
        {
            LoggerManager.Current.Write($"BLL Factura - Validando buscar factura por nombre de factura exacto", EventLevel.Informational);
            try
            {
                facturas = GetAll(obj).ToList();
                //Busco factura que en la número de factura contengan los valores ingresados por el usuario
                if (facturas.Any(o => o.Numero_Factura.Equals(obj.Numero_Factura)))
                {
                    return (from o in facturas
                            where o.Numero_Factura == obj.Numero_Factura
                            select o).FirstOrDefault();
                }
                else
                {
                    throw new Exception($"Ninguna factura contiene en el número \"{obj.Numero_Factura}\"");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Factura - Error al buscar factura por número de factura exacto en: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }


        public List<Factura> BuscarFacturaxFechaFacturaExacto(Factura obj)
        {
            LoggerManager.Current.Write($"BLL Factura - Validando buscar factura por fecha de factura", EventLevel.Informational);
            try
            {
                facturas = GetAll(obj).ToList();
                //Busco factura que la fecha de factura contengan los valores ingresados por el usuario
                if (facturas.Any(o => o.Fecha_Alta_Factura.Date.Equals(obj.Fecha_Alta_Factura.Date)))
                {
                    return (from o in facturas
                            where o.Fecha_Alta_Factura.Date == obj.Fecha_Alta_Factura.Date
                            select o).ToList();
                }
                else
                {
                    throw new Exception($"Ninguna factura contiene la fecha  \"{obj.Fecha_Alta_Factura.Date}\"");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Factura - Error al buscar factura por fecha de factura en: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }


        public List<Factura> BuscarFacturaxCliente(Factura obj)
        {
            LoggerManager.Current.Write($"BLL Factura - Validando buscar factura por número de cliente de factura", EventLevel.Informational);
            try
            {
                facturas = GetAll(obj).ToList();
                //Busco factura por número de cliente de factura que contengan los valores ingresados por el usuario
                if (facturas.Any(o => o.Cliente.Id_Cliente.Equals(obj.Cliente.Id_Cliente)))
                {
                    return (from o in facturas
                            where o.Cliente.Id_Cliente == obj.Cliente.Id_Cliente
                            select o).ToList();
                }
                else
                {
                    throw new Exception($"Ninguna factura contiene el número de cliente:\"{obj.Cliente.Numero_Cliente}\"");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Factura - Error al buscar factura por número de cliente: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }



        public List<Factura> BuscarFacturaxEstadoFactura(Factura obj)
        {
            LoggerManager.Current.Write($"BLL Factura - Validando buscar factura por estado de factura", EventLevel.Informational);
            try
            {
                facturas = GetAll(obj).ToList();
                //Busco factura por estado de factura que contengan los valores ingresados por el usuario
                if (facturas.Any(o => o.Estado.Equals(obj.Estado)))
                {
                    return (from o in facturas
                            where o.Estado == obj.Estado
                            select o).ToList();
                }
                else
                {
                    throw new Exception($"Ninguna factura contiene el estado de factura:\"{obj.Estado}\"");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Factura - Error al buscar factura por estado de factur: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }



        public Factura BuscarFacturaxNumeroPedido(Pedido obj)
        {
            LoggerManager.Current.Write($"BLL Facturas - Validando buscar factura por número de pedido", EventLevel.Informational);
            try
            {
                //Busco factura por número de pedido que contengan los valores ingresados por el usuario
                return BuscarFacturaxNumeroFacturaExacto(Factura_PedidoBusinessLogic.Current.BuscarFacturaxNumeroPedido(new Factura_Pedido { Id_Empresa = obj.Id_Empresa, Id_Sucursal = obj.Id_Sucursal, Pedido = obj }));
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Factura - Error al buscar factura por número de pedido: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }


    }
}
