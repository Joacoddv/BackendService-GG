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
    public sealed class PedidoBusinessLogic : IGenericBusinessLogic<Pedido>
    {

        private readonly static PedidoBusinessLogic _instance = new PedidoBusinessLogic();

        List<Pedido> pedidos = new List<Pedido>();

        IGenericRepository<Pedido> PedidoRepository = Factory.Current.GetPedidoRepository();

        public static PedidoBusinessLogic Current
        {
            get
            {
                return _instance;
            }
        }

        private PedidoBusinessLogic()
        {
            //Implent here the initialization of your singleton
            //pedidos = GetAll(pedido).ToList();

        }
        public void Add(Pedido obj)
        {
            try
            {
                LoggerManager.Current.Write($"BLL Pedido - Validando Tipo de de pedido en Alta de pedido", EventLevel.Informational);
                if (obj.Tipo_Pedido == ETipo_Pedido.Pedido_Salon)
                {
                    obj.Direccion = new Direccion();
                    obj.Cliente = new Cliente();
                }
                if (obj.Tipo_Pedido == ETipo_Pedido.Pedido_Delivery)
                {
                    obj.Mesa = new Mesa();
                }
                if (obj.Tipo_Pedido == ETipo_Pedido.Pedidoi_Take_Away)
                {
                    obj.Direccion = new Direccion();
                    obj.Mesa = new Mesa();
                }
                PedidoRepository.Insert(obj);
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"Error al dar de alta Plato en BLL Plato: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }

        }








        public IEnumerable<Pedido> GetAll(Pedido pedido)
        {
            try
            {
                LoggerManager.Current.Write($"BLL Pedido - Validando buscar pedidos", EventLevel.Informational);
                return from o in PedidoRepository.GetAll(pedido)
                       orderby o.Numero_Pedido descending
                       select o;
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"Error al dar buscar pedido en BLL pedido: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }

        public Pedido GetOne(Pedido pedido)
        {
            try
            {
                LoggerManager.Current.Write($"BLL Pedido - Validando buscar un pedido", EventLevel.Informational);
                return PedidoRepository.GetOne(pedido);
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"Error al dar buscar un pedido en BLL pedido: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }

        public void Remove(Pedido pedido)
        {
            throw new NotImplementedException();
        }

        public void Update(Pedido pedido)
        {
            try
            {
                LoggerManager.Current.Write($"BLL Pedido - Validando actualizacion de pedido", EventLevel.Informational);
                pedidos = GetAll(pedido).ToList();
                if ((from o in pedidos
                     where o.Id_Pedido == pedido.Id_Pedido
                     select o).Any() == true)
                {
                    LoggerManager.Current.Write($"BLL Pedido - Validando Tipo de de pedido en Alta de pedido", EventLevel.Informational);
                    if (pedido.Tipo_Pedido == ETipo_Pedido.Pedido_Salon)
                    {
                        pedido.Direccion = new Direccion();
                        pedido.Cliente = new Cliente();
                    }
                    if (pedido.Tipo_Pedido == ETipo_Pedido.Pedido_Delivery)
                    {
                        pedido.Mesa = new Mesa();
                    }
                    if (pedido.Tipo_Pedido == ETipo_Pedido.Pedidoi_Take_Away)
                    {
                        pedido.Direccion = new Direccion();
                        pedido.Mesa = new Mesa();
                    }
                    PedidoRepository.Update(pedido);
                    pedidos = GetAll(pedido).ToList();
                }
                else
                {
                    throw new Exception("No existe el pedido que quiere actualizar.");
                }


            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Pedido: - Error al actualizar pedido: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }

        public Pedido HidratarPedido(Pedido pedido)
        {
            try
            {
                pedidos = GetAll(pedido).ToList();
                if (pedido == null && GetOne(pedido) != null)
                {
                    LoggerManager.Current.Write($"BLL Pedido - Validando hidratar cliente de pedido", EventLevel.Informational);
                    if (pedido.Cliente.Id_Cliente != null)
                    {
                        pedido.Cliente = ClienteBusinessLogic.Current.GetOne(pedido.Cliente);
                    }
                    LoggerManager.Current.Write($"BLL Pedido - Validando hidratar direccion de pedido", EventLevel.Informational);
                    if (pedido.Direccion.Id_Direccion != null)
                    {
                        pedido.Direccion = DireccionBusinessLogic.Current.GetOne(pedido.Direccion);
                    }
                    LoggerManager.Current.Write($"BLL Pedido - Validando hidratar mesa de pedido", EventLevel.Informational);
                    if (pedido.Mesa.Id_Mesa != null)
                    {
                        pedido.Mesa = MesaBusinessLogic.Current.GetOne(pedido.Mesa);
                    }
                    return pedido;
                }
                else
                {
                    throw new Exception("No se encuentra el pedido a hidratar.");
                }


            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Pedido: - Error al hidratar pedido: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }

        }


        public List<EEstadoPedido> ValidarEstadosPosibles(Pedido obj)
        {
            try
            {
                LoggerManager.Current.Write($"BLL Pedido - Validando estados posibles de pedido", EventLevel.Informational);
                List<EEstadoPedido> ListaPosiblesEstados = new List<EEstadoPedido>();
                switch (obj.Estado)
                {
                    case EEstadoPedido.Creado:
                        ListaPosiblesEstados.Add(EEstadoPedido.Preparandose);
                        ListaPosiblesEstados.Add(EEstadoPedido.Cancelado);
                        break;
                    case EEstadoPedido.Modificado:
                        ListaPosiblesEstados.Add(EEstadoPedido.Preparandose);
                        ListaPosiblesEstados.Add(EEstadoPedido.Cancelado);
                        break;
                    case EEstadoPedido.Preparandose:
                        ListaPosiblesEstados.Add(EEstadoPedido.Listo);
                        break;
                    case EEstadoPedido.Listo:
                        ListaPosiblesEstados.Add(EEstadoPedido.Entregado);
                        break;
                    case EEstadoPedido.Entregado:
                        break;
                    case EEstadoPedido.Cancelado:
                        break;
                    default:
                        break;
                }
                return ListaPosiblesEstados;
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Pedido: - Error al Validar estado posibles de pedido: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }








        public List<Pedido> BuscarPedidosxNumeroPedido(Pedido obj)
        {
            //Busco  pedido a partir del numero pedido
            LoggerManager.Current.Write($"BLL Pédido - Validando buscar pedido por número pedido", EventLevel.Informational);
            List<Pedido> pedidosxnumeropedido = new List<Pedido>();
            try
            {
                pedidos = GetAll(obj).ToList();
                //Busco pedido que en el número pedido contengan los valores ingresados por el usuario
                if (pedidos.Any(o => o.Numero_Pedido.ToString().Trim().Contains(obj.Numero_Pedido.ToString().Trim())))
                {
                    pedidosxnumeropedido = (from o in pedidos
                                            where o.Numero_Pedido.ToString().Trim().Contains(obj.Numero_Pedido.ToString().Trim())
                                            select o).ToList();
                }
                else
                {
                    throw new Exception($"Ningun pedido contiene en su número de pedido \"{obj.Numero_Pedido}\"");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Pedido - Error al buscar pedido por número de pedido: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
            return pedidosxnumeropedido;
        }


        public Pedido BuscarPedidoxNumeroPedidoExacto(Pedido obj)
        {
            LoggerManager.Current.Write($"BLL Platos - Validando buscar plato por nombre de plato exacto", EventLevel.Informational);
            try
            {
                pedidos = GetAll(obj).ToList();
                //Busco platos que en la número de pedido contengan los valores ingresados por el usuario
                if (pedidos.Any(o => o.Numero_Pedido.Equals(obj.Numero_Pedido)))
                {
                    return (from o in pedidos
                            where o.Numero_Pedido == obj.Numero_Pedido
                            select o).FirstOrDefault();
                }
                else
                {
                    throw new Exception($"Ningun pedido contiene en el número \"{obj.Numero_Pedido}\"");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Platos - Error al buscar plato por número de plato exacto en: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }


        public List<Pedido> BuscarPedidosDisponiblesparaFacturarxxIdCliente(Pedido obj)
        {
            //Busco pedido a partir del numero de cliente y que este disponible para facturar 
            LoggerManager.Current.Write($"BLL Pédido - Validando buscar pedido por cliente y estado No-Facturado", EventLevel.Informational);
            List<Pedido> pedidosparafacturar = new List<Pedido>();
            try
            {
                pedidos = GetAll(obj).ToList();
                //Busco pedido que en el número pedido contengan los valores ingresados por el usuario
                if (pedidos.Any(o => o.Cliente.Id_Cliente.Equals(obj.Cliente.Id_Cliente) && o.Estado_Factura_Pedido == EEstadoFacturaPedido.No_Facturado))
                {
                    pedidosparafacturar = (from o in pedidos
                                           where o.Cliente != null &&
                                                 obj.Cliente != null &&
                                                 o.Cliente.Id_Cliente != null &&
                                                 obj.Cliente.Id_Cliente != null &&
                                                 o.Cliente.Id_Cliente.Equals(obj.Cliente.Id_Cliente) &&
                                                 o.Estado_Factura_Pedido == EEstadoFacturaPedido.No_Facturado
                                           select o).ToList();
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Pedido - Error al buscar pedido por cliente y estado No-Facturado: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
            return pedidosparafacturar;
        }

        public List<Pedido> BuscarPedidosxCliente(Pedido obj)
        {
            //Busco  pedido a partir del numero cliente
            LoggerManager.Current.Write($"BLL Pédido - Validando buscar pedido por número cliente", EventLevel.Informational);
            try
            {
                pedidos = GetAll(obj).ToList();
                //Busco pedido que en el número cliente contengan los valores ingresados por el usuario
                if (pedidos.Any(o => o.Cliente.Id_Cliente != null && obj.Cliente.Id_Cliente != null &&
                                  o.Cliente.Id_Cliente.ToString().Trim().Equals(obj.Cliente.Id_Cliente.ToString().Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    return (from o in pedidos
                            where o.Cliente != null &&
                                  o.Cliente.Id_Cliente != Guid.Empty &&
                                  obj.Cliente != null &&
                                  obj.Cliente.Id_Cliente != null &&
                                  o.Cliente.Id_Cliente.Equals(obj.Cliente.Id_Cliente)
                            select o).ToList();
                }
                else
                {
                    throw new Exception($"Ningun pedido contiene esecliente \"{obj.Cliente.Id_Cliente}\"".Traducir());
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Pedido - Error al buscar pedido por cliente: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }


        public List<Pedido> BuscarPedidosxDireccion(Pedido obj)
        {
            //Busco  pedido a partir de direccion
            LoggerManager.Current.Write($"BLL Pédido - Validando buscar pedido por direccion", EventLevel.Informational);
            try
            {
                pedidos = GetAll(obj).ToList();
                //Busco pedido que en la direccion contengan los valores ingresados por el usuario
                if (pedidos.Any(o => o.Direccion.Id_Direccion != null && obj.Direccion.Id_Direccion != null &&
                                  o.Direccion.Id_Direccion.ToString().Trim().Equals(obj.Direccion.Id_Direccion.ToString().Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    return (from o in pedidos
                            where o.Direccion != null &&
                                  o.Direccion.Id_Direccion != Guid.Empty &&
                                  obj.Direccion != null &&
                                  obj.Direccion.Id_Direccion != null &&
                                  o.Direccion.Id_Direccion.Equals(obj.Direccion.Id_Direccion)
                            select o).ToList();
                }
                else
                {
                    throw new Exception($"Ningun pedido contiene la direccion \"{obj.Direccion.Nombre_Calle}\" \"{obj.Direccion.Altura}\"".Traducir());
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Pedido - Error al buscar pedido por direccion: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }


        public List<Pedido> BuscarPedidosxMesa(Pedido obj)
        {
            //Busco  pedido a partir de mesa
            LoggerManager.Current.Write($"BLL Pédido - Validando buscar pedido por mesa", EventLevel.Informational);
            try
            {
                pedidos = GetAll(obj).ToList();
                //Busco pedido que en la mesa contengan los valores ingresados por el usuario
                if (pedidos.Any(o =>
                    o.Mesa != null &&
                    o.Mesa.Id_Mesa != null &&
                    obj.Mesa != null &&
                    obj.Mesa.Id_Mesa != null &&
                    o.Mesa.Id_Mesa.ToString().Trim().Equals(obj.Mesa.Id_Mesa.ToString().Trim(), StringComparison.OrdinalIgnoreCase)))
                {
                    return (from o in pedidos
                            where o.Mesa != null &&
                                  o.Mesa.Id_Mesa != Guid.Empty &&
                                  obj.Mesa != null &&
                                  obj.Mesa.Id_Mesa != null &&
                                  o.Mesa.Id_Mesa.Equals(obj.Mesa.Id_Mesa)
                            select o).ToList();
                }
                else
                {
                    throw new Exception($"Ningun pedido contiene la mesa o la misma no existe.");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Pedido - Error al buscar pedido por mesa: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }

        //public List<Pedido> BuscarPedidosxNumeroClienteExacto(Pedido obj)
        //{
        //    LoggerManager.Current.Write($"BLL Platos - Validando buscar plato por nombre de cliente exacto", EventLevel.Informational);
        //    try
        //    {
        //        //Busco platos que en la número de cliente contengan los valores ingresados por el usuario
        //        if (pedidos.Any(o => o.Cliente.Numero_Cliente.Equals(obj.Cliente.Numero_Cliente)))
        //        {
        //            return (from o in pedidos
        //                    where o.Cliente.Numero_Cliente == obj.Cliente.Numero_Cliente
        //                    select o).ToList();
        //        }
        //        else
        //        {
        //            throw new Exception($"Ningun pedido contiene en el número de cliente \"{ obj.Cliente.Numero_Cliente}\"");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        LoggerManager.Current.Write($"BLL Platos - Error al buscar plato por número de cliente exacto en: {ex.Message}", EventLevel.Error);
        //        throw new Exception(ex.Message);
        //    }
        //}

        //public List<Pedido> BuscarPedidosxNumeroClienteExactoSinElse(Pedido obj)
        //{
        //    LoggerManager.Current.Write($"BLL Platos - Validando buscar plato por nombre de cliente exacto", EventLevel.Informational);
        //    try
        //    {
        //        List<Pedido> pedidosxnumerocliente = new List<Pedido>();
        //        //Busco platos que en la número de cliente contengan los valores ingresados por el usuario
        //        if (pedidos.Any(o => o.Cliente.Numero_Cliente.Equals(obj.Cliente.Numero_Cliente)))
        //        {
        //            pedidosxnumerocliente = (from o in pedidos
        //                                     where o.Cliente.Numero_Cliente == obj.Cliente.Numero_Cliente
        //                                     select o).ToList();
        //        }
        //        return pedidosxnumerocliente;
        //    }
        //    catch (Exception ex)
        //    {
        //        LoggerManager.Current.Write($"BLL Platos - Error al buscar plato por número de cliente exacto en: {ex.Message}", EventLevel.Error);
        //        throw new Exception(ex.Message);
        //    }
        //}



        public List<Pedido> BuscarPedidosxEstadoPedido(Pedido obj)
        {
            LoggerManager.Current.Write($"BLL Pedido - Validando buscar pedido por estado de pedido", EventLevel.Informational);
            try
            {
                pedidos = GetAll(obj).ToList();
                //Busco pedidos a partir del estado del pedido
                if (pedidos.Any(o => o.Estado.Equals(obj.Estado)))
                {
                    return (from o in pedidos
                            where o.Estado == obj.Estado
                            select o).ToList();
                }
                else
                {
                    throw new Exception($"Ningun pedido contiene el estado pedido: \"{obj.Estado}\"");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Pedido - Error al buscar pedido por estado pedido: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }


        public List<Pedido> BuscarPedidosxEstadoFacturaPedido(Pedido obj)
        {
            LoggerManager.Current.Write($"BLL Pedido - Validando buscar pedido por estado factura-pedido en pedido", EventLevel.Informational);
            try
            {
                pedidos = GetAll(obj).ToList();
                //Busco pedidos a partir del estado factura-pedido del pedido
                if (pedidos.Any(o => o.Estado_Factura_Pedido.Equals(obj.Estado_Factura_Pedido)))
                {
                    return (from o in pedidos
                            where o.Estado_Factura_Pedido == obj.Estado_Factura_Pedido
                            select o).ToList();
                }
                else
                {
                    throw new Exception($"Ningun pedido contiene el estado factura-pedido en el pedido: \"{obj.Estado_Factura_Pedido}\"");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Pedido - Error al buscar pedido por estado factura-pedido en pedido: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }


        public List<Pedido> BuscarPedidosxFechaPedido(Pedido obj)
        {
            LoggerManager.Current.Write($"BLL Pedido - Validando buscar pedido por fecha de pedido", EventLevel.Informational);
            try
            {
                pedidos = GetAll(obj).ToList();
                //Busco pedidos a partir de la fecha del pedido
                if (pedidos.Any(o => o.Fecha_Creacion.Date.Equals(obj.Fecha_Creacion.Date)))
                {
                    return (from o in pedidos
                            where o.Fecha_Creacion.Date == obj.Fecha_Creacion.Date
                            select o).ToList();
                }
                else
                {
                    throw new Exception($"Ningun pedido contiene la fecha de pedido: \"{obj.Fecha_Creacion.Date}\"");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Pedido - Error al buscar pedido por fecha pedido: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }


        public List<Pedido> BuscarPedidosxFechaEntregaPedido(Pedido obj)
        {
            LoggerManager.Current.Write($"BLL Pedido - Validando buscar pedido por fecha entrega de pedido", EventLevel.Informational);
            try
            {
                pedidos = GetAll(obj).ToList();
                //Busco pedidos a partir de la fecha de entrega del pedido
                if (pedidos.Any(o => o.Fecha_Entrega.Date.Equals(obj.Fecha_Entrega.Date)))
                {
                    return (from o in pedidos
                            where o.Fecha_Entrega.Date == obj.Fecha_Entrega.Date
                            select o).ToList();
                }
                else
                {
                    throw new Exception($"Ningun pedido contiene la fecha entrega de pedido: \"{obj.Fecha_Entrega.Date}\"");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Pedido - Error al buscar pedido por fecha entrega de pedido: {ex.Message}", EventLevel.Error);
                throw new Exception(ex.Message);
            }
        }


        //public int BuscarPedidosxFechaEntregaPedidoSinElse(Pedido obj)
        //{
        //    LoggerManager.Current.Write($"BLL Pedido - Validando buscar pedido por fecha entrega de pedido sin else", EventLevel.Informational);
        //    try
        //    {
        //        pedidos = GetAll(obj).ToList();
        //        //Busco pedidos a partir de la fecha de entrega del pedido
        //        if (pedidos.Any(o => o.Fecha_Entrega.Date.Equals(obj.Fecha_Entrega.Date)))
        //        {
        //            return (from o in pedidos
        //                    where o.Fecha_Entrega.Date == obj.Fecha_Entrega.Date
        //                    select o).ToList().Count;
        //        }
        //        else
        //        {
        //            return 0;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        LoggerManager.Current.Write($"BLL Pedido - Error al buscar pedido por fecha entrega de pedido sin else: {ex.Message}", EventLevel.Error);
        //        throw new Exception(ex.Message);
        //    }
        //}

        //public List<Pedido> BuscarPedidosxNombreCliente(Pedido obj)
        //{
        //    //Busco  pedido a partir del nombre del cliente
        //    LoggerManager.Current.Write($"BLL Pédido - Validando buscar pedido por nombre de cliente", EventLevel.Informational);
        //    List<Pedido> pedidosxnombrecliente = new List<Pedido>();
        //    try
        //    {
        //        foreach (var item in ClienteBusinessLogic.Current.BuscarClientesxNombreCliente(obj.Cliente))
        //        {
        //            //BuscarClientesxNombreClienteSinElse
        //            if (BuscarPedidosxNumeroClienteExactoSinElse(new Pedido { Cliente = item }).Any())
        //            {
        //                foreach (var item2 in BuscarPedidosxNumeroClienteExacto(new Pedido { Cliente = item }))
        //                {
        //                    pedidosxnombrecliente.Add(item2);
        //                }
        //            }
        //        }
        //        return pedidosxnombrecliente;
        //    }
        //    catch (Exception ex)
        //    {
        //        LoggerManager.Current.Write($"BLL Pedido - Error al buscar pedido por nombre de cliente: {ex.Message}", EventLevel.Error);
        //        throw new Exception(ex.Message);
        //    }

        //}


        //public List<Pedido> BuscarPedidosxApellidoCliente(Pedido obj)
        //{
        //    //Busco  pedido a partir del nombre del cliente
        //    LoggerManager.Current.Write($"BLL Pédido - Validando buscar pedido por apellido de cliente", EventLevel.Informational);
        //    List<Pedido> pedidosxapellidocliente = new List<Pedido>();
        //    try
        //    {
        //        foreach (var item in ClienteBusinessLogic.Current.BuscarClientesxApellidoCliente(obj.Cliente))
        //        {

        //            if (BuscarPedidosxNumeroClienteExactoSinElse(new Pedido { Cliente = item }).Any())
        //            {
        //                foreach (var item2 in BuscarPedidosxNumeroClienteExacto(new Pedido { Cliente = item }))
        //                {
        //                    pedidosxapellidocliente.Add(item2);
        //                }
        //            }
        //        }
        //        return pedidosxapellidocliente;
        //    }
        //    catch (Exception ex)
        //    {
        //        LoggerManager.Current.Write($"BLL Pedido - Error al buscar pedido por apellido de cliente: {ex.Message}", EventLevel.Error);
        //        throw new Exception(ex.Message);
        //    }

        //}

    }
}
