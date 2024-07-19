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
    public sealed class Factura_PedidoBusinessLogic : IGenericBusinessLogic<Factura_Pedido>
    {
        private readonly static Factura_PedidoBusinessLogic _instance = new Factura_PedidoBusinessLogic();

        private List<Factura_Pedido> facturaspedidos = new List<Factura_Pedido>();

        IGenericRepository<Factura_Pedido> Factura_Pedido_Repository = Factory.Current.GetFactura_PedidoRepository();

        public static Factura_PedidoBusinessLogic Current
        {
            get
            {
                return _instance;
            }
        }

        private Factura_PedidoBusinessLogic()
        {
            //facturaspedidos = GetAll().ToList();
            //Implent here the initialization of your singleton
        }

        public void Add(Factura_Pedido obj)
        {
            Factura_Pedido_Repository.Insert(obj);
            facturaspedidos = GetAll(obj).ToList();
        }

        public void Remove(Factura_Pedido obj)
        {
            Factura_Pedido_Repository.Delete(obj);
            facturaspedidos = GetAll(obj).ToList();
        }

        public void Update(Factura_Pedido obj)
        {
            Factura_Pedido_Repository.Update(obj);
        }

        public IEnumerable<Factura_Pedido> GetAll(Factura_Pedido obj)
        {
            return Factura_Pedido_Repository.GetAll(obj);
        }

        public Factura_Pedido GetOne(Factura_Pedido obj)
        {
            return Factura_Pedido_Repository.GetOne(obj);
        }


        public void EstadoFacturadoPedidosdelaFactura(Factura obj)
        {
            LoggerManager.Current.Write($"BLL Facturas_Pedido - Actualizando estado pedidos en la factura a Facturado", EventLevel.Informational);
            try
            {
                //Busco pedidos que se encuentran dentro de la factura
                foreach (var item in Factura_PedidosxNumeroFactura(obj))
                {
                    item.Pedido = PedidoBusinessLogic.Current.BuscarPedidoxNumeroPedidoExacto(item.Pedido);
                    item.Pedido.Estado_Factura_Pedido = EEstadoFacturaPedido.Facturado;
                    PedidoBusinessLogic.Current.Update(item.Pedido);
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Facturas_Pedido - Error al actualizar estado pedidos en la factura a Facturado: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }


        public void EstadoNOFacturadoPedidosdelaFactura(Factura obj)
        {
            LoggerManager.Current.Write($"BLL Facturas_Pedido - Actualizando estado pedidos en la factura a No Facturado", EventLevel.Informational);
            try
            {
                //Busco pedidos que se encuentran dentro de la factura
                foreach (var item in Factura_PedidosxNumeroFactura(obj))
                {
                    item.Pedido = PedidoBusinessLogic.Current.BuscarPedidoxNumeroPedidoExacto(item.Pedido);
                    item.Pedido.Estado_Factura_Pedido = EEstadoFacturaPedido.No_Facturado;
                    PedidoBusinessLogic.Current.Update(item.Pedido);
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Facturas_Pedido - Error al actualizar estado pedidos en la factura a No Facturado: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }

        public void EstadoPagadoPedidosdelaFactura(Factura obj)
        {
            LoggerManager.Current.Write($"BLL Facturas_Pedido - Actualizando estado pedidos en la factura a Pagado", EventLevel.Informational);
            try
            {
                //Busco pedidos que se encuentran dentro de la factura
                foreach (var item in Factura_PedidosxNumeroFactura(obj))
                {
                    item.Pedido = PedidoBusinessLogic.Current.BuscarPedidoxNumeroPedidoExacto(item.Pedido);
                    item.Pedido.Estado_Factura_Pedido = EEstadoFacturaPedido.Pagado;
                    PedidoBusinessLogic.Current.Update(item.Pedido);
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Facturas_Pedido - Error al actualizar estado pedidos en la factura a Pagado: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }


        public bool ValidarPedidoenFactura(Factura_Pedido obj)
        {
            LoggerManager.Current.Write($"BLL Facturas_Pedido - Validando existencia de pedido en factura", EventLevel.Informational);
            try
            {
                // Devuelvo true si esta todo ok
                bool estado = true;
                //Busco pedidos que se encuentran dentro de la factura
                foreach (var item in Factura_PedidosxNumeroFactura(obj.Factura))
                {
                    if (item.Pedido.Numero_Pedido == obj.Pedido.Numero_Pedido)
                    {
                        estado = false;
                    }
                }
                return estado;
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Facturas_Pedido - Error al validar existencia de pedido en factura: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }


        public Factura BuscarFacturaxNumeroPedido(Factura_Pedido obj)
        {
            LoggerManager.Current.Write($"BLL Facturas_Pedido - Validando buscar factura_pedido por número de pedido en factura_pedido", EventLevel.Informational);
            try
            {
                //Busco factura_pedido por número de pedido que contengan los valores ingresados por el usuario
                if (facturaspedidos.Any(o => o.Pedido.Numero_Pedido.Equals(obj.Pedido.Numero_Pedido)))
                {
                    return (from o in facturaspedidos
                            where o.Pedido.Numero_Pedido == obj.Pedido.Numero_Pedido
                            select o.Factura).FirstOrDefault();
                }
                else
                {
                    throw new Exception($"Ninguna factura contiene el pedido:\"{ obj.Pedido.Numero_Pedido}\"");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Facturas_Pedido - Error al buscar factura_pedido por número de pedido: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }








        public List<Factura_Pedido> Factura_PedidosxNumeroFactura(Factura factura)
        {
            List<Factura_Pedido> facturapedidosenfactura = new List<Factura_Pedido>();

            foreach (var item in facturaspedidos)
            {
                if (item.Factura.Numero_Factura == factura.Numero_Factura)
                {
                    item.Pedido = PedidoBusinessLogic.Current.BuscarPedidoxNumeroPedidoExacto(item.Pedido);
                    facturapedidosenfactura.Add(item);
                }
            }
            return facturapedidosenfactura;
        }





        public List<Pedido> BuscarPedidosenFacturaxFactura(Factura obj)
        {
            List<Pedido> pedidosenfactura = new List<Pedido>();

            foreach (var item in facturaspedidos)
            {
                if (item.Factura.Numero_Factura == obj.Numero_Factura)
                {
                    pedidosenfactura.Add(item.Pedido);
                }
            }
            return pedidosenfactura;
        }


        public decimal CalcularSubTotal(Factura obj)
        {
            //Sumo todos los precios de los platos asignados a un pedido a partir de una fecha
            decimal Monto = 0;
            foreach (var item in BuscarPedidosenFacturaxFactura(obj))
            {
                item.Monto = PedidoBusinessLogic.Current.BuscarPedidoxNumeroPedidoExacto(item).Monto;
                Monto += item.Monto;
            }
            return Monto;

        }

        public decimal CalcularIvaFactura(Factura obj)
        {
            return Decimal.Multiply(CalcularSubTotal(obj), (decimal)0.21);
        }

        public decimal CalcularTotalFactura(Factura obj)
        {
            return CalcularSubTotal(obj) + CalcularIvaFactura(obj);
        }

    }
}
