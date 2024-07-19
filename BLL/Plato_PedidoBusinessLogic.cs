using System;
using System.Collections.Generic;
using System.Linq;
using BLL.Contracts;
using DAL.Contracts;
using Dominio;
using System.Diagnostics.Tracing;
using DLL.Repositories.SqlServer;
using Servicios.Services;

namespace BLL
{
    public sealed class Plato_PedidoBusinessLogic : IGenericBusinessLogic<Plato_Pedido>
    {



        private readonly static Plato_PedidoBusinessLogic _instance = new Plato_PedidoBusinessLogic();

        Plato_PedidoRepository Plato_PedidoRepository = new Plato_PedidoRepository();

        public static Plato_PedidoBusinessLogic Current
        {
            get
            {
                return _instance;
            }
        }

        private Plato_PedidoBusinessLogic()
        {
            //Implent here the initialization of your singleton
            //plato_pedidos = Plato_PedidoRepository.GetAll().ToList();
        }

        public void Add(Plato_Pedido plato_pedido)
        {
            try
            {
                LoggerManager.Current.Write($"BLL Plato Pedido - Validando alta de plato en pedido", EventLevel.Informational);

                if (ValidatePlatoPedido(plato_pedido) == false)
                {

                    Plato_PedidoRepository.Insert(plato_pedido);
                }
                else
                {
                    throw new Exception("Ya existe el plato en el pedido. Agregue Cantidad");
                }
            }
            catch (Exception ex)
            {
                HandleException("Error al dar de alta plato en pedido", ex);
            }
        }

        public void Remove(Plato_Pedido plato_pedido)
        {
            try
            {
                LoggerManager.Current.Write($"BLL Plato Pedido - Validando baja de plato en pedido", EventLevel.Informational);


                if (ValidatePlatoPedido(plato_pedido) == true)
                {
                    Plato_PedidoRepository.Delete(plato_pedido);

                }
                else
                {
                    throw new Exception("No existe el plato del pedido que desea eliminar");
                }
            }
            catch (Exception ex)
            {
                HandleException("Error al dar de baja plato en pedido", ex);
            }
        }

        public void Update(Plato_Pedido plato_pedido)
        {
            try
            {
                LoggerManager.Current.Write($"BLL Plato Pedido - Validando modificacion de plato en pedido", EventLevel.Informational);

                if (ValidatePlatoPedido(plato_pedido) == true)
                {
                    Plato_PedidoRepository.Update(plato_pedido);
                }
                else
                {
                    throw new Exception("No existe el plato del pedido que desea actualizar");
                }


            }
            catch (Exception ex)
            {
                HandleException("Error al modificar plato en pedido", ex);
            }
        }

        public IEnumerable<Plato_Pedido> GetAll(Plato_Pedido plato_pedido)
        {
            try
            {
                LoggerManager.Current.Write($"BLL Plato Pedido - Buscando platos en pedidos", EventLevel.Informational);
                return Plato_PedidoRepository.GetAll(plato_pedido);
            }
            catch (Exception ex)
            {
                HandleException("Error al buscar platos en pedidos", ex);
                throw;
            }
        }

        public Plato_Pedido GetOne(Plato_Pedido plato_pedido)
        {
            try
            {
                LoggerManager.Current.Write($"BLL Plato Pedido - Validando buscar un plato en pedido", EventLevel.Informational);
                return Plato_PedidoRepository.GetOne(plato_pedido);
            }
            catch (Exception ex)
            {
                HandleException("Error al buscar un plato en pedido", ex);
                throw;
            }
        }

        public IEnumerable<Plato_Pedido> GetOnePedido(Plato_Pedido plato_pedido)
        {
            try
            {
                LoggerManager.Current.Write($"BLL Plato Pedido - Validando buscar platos de un pedido", EventLevel.Informational);
                return Plato_PedidoRepository.GetOnePedido(plato_pedido);
            }
            catch (Exception ex)
            {
                HandleException("Error al buscar platos de un pedido", ex);
                throw;
            }
        }

        public bool ValidarPlatoEnPedido(Plato_Pedido plato_pedido)
        {
            try
            {
                LoggerManager.Current.Write($"BLL Plato Pedido - Validando si ya existe plato en pedido", EventLevel.Informational);
                return BuscarPlatoPedidoxPedido(plato_pedido).All(item => item.Plato.Id_Plato != plato_pedido.Plato.Id_Plato);
            }
            catch (Exception ex)
            {
                HandleException("Error al validar si existe plato en pedido", ex);
                throw;
            }
        }

        public List<Plato_Pedido> BuscarPlatoPedidoxPedido(Plato_Pedido plato_pedido)
        {
            try
            {
                LoggerManager.Current.Write($"BLL Plato Pedido - Validando buscar plato pedido por pedido", EventLevel.Informational);
                var plato_pedidos = Plato_PedidoRepository.GetAll(plato_pedido).ToList();

                return plato_pedidos
                    .Where(item => item.Pedido.Id_Pedido == plato_pedido.Pedido.Id_Pedido)
                    .Select(item =>
                    {
                        item.Plato = PlatoBusinessLogic.Current.GetOne(item.Plato);
                        return item;
                    })
                    .ToList();
            }
            catch (Exception ex)
            {
                HandleException("Error al buscar plato pedido por pedido", ex);
                throw;
            }
        }

        public decimal CalcularMontoPedido(Plato_Pedido obj)
        {
            try
            {
                LoggerManager.Current.Write($"BLL Plato Pedido - Calculando monto del pedido", EventLevel.Informational);

                decimal Monto = 0;
                var plato_pedidos = BuscarPlatoPedidoxPedido(obj);

                foreach (var item in plato_pedidos)
                {
                    item.Pedido = PedidoBusinessLogic.Current.GetOne(obj.Pedido);
                    item.Plato = PlatoBusinessLogic.Current.GetOne(obj.Plato);
                    Monto += MenuBusinessLogic.Current.BuscarPrecioMenudelDiaoPrecioVIgentexPlatoyFecha(new Menu { Id_Empresa = item.Id_Empresa, Id_Sucursal = item.Id_Sucursal, Plato = item.Plato, Fecha_Dia_Menu = item.Pedido.Fecha_Entrega }) * item.Cantidad;
                }

                return Monto;
            }
            catch (Exception ex)
            {
                HandleException("Error al calcular monto del pedido", ex);
                throw;
            }
        }

        private bool ValidatePlatoPedido(Plato_Pedido plato_pedido)
        {
            var plato_pedidos = Plato_PedidoRepository.GetAll(plato_pedido).ToList();

            if (plato_pedidos.Any(o =>
                o.Id_Plato_Pedido == plato_pedido.Id_Plato_Pedido &&
                o.Plato.Id_Plato == plato_pedido.Plato.Id_Plato &&
                o.Pedido.Id_Pedido == plato_pedido.Pedido.Id_Pedido &&
                o.Id_Sucursal == plato_pedido.Id_Sucursal &&
                o.Id_Empresa == o.Id_Empresa))
            {
                return true;
            }
            else
                return false;
        }

        private void HandleException(string message, Exception ex)
        {
            LoggerManager.Current.Write($"BLL Plato Pedido: - {message}: {ex.Message}", EventLevel.Error);
            throw ex;
        }
    }
}
