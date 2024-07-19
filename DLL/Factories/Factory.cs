using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.Contracts;
using DAL.Repositories.SqlServer;
using DLL.Repositories.SqlServer;
using Dominio;

namespace DAL.Factories
{
    public sealed class Factory
    {
        //string backend;
        //public void SetearAccesoDatos(string backend)
        //{
        //    this.backend = backend;
        //}

        #region Singleton
        private readonly static Factory _instance = new Factory();
        private string backend;
        public static Factory Current
        {
            get
            {
                return _instance;
            }
        }

        private Factory()
        {
            //Implement here the initialization code
            //backend = ConfigurationManager.AppSettings["backend"];
            backend = "SQL";
        }
        #endregion

        public IGenericRepository<Cliente> GetClienteRepository()
        {
            return new ClienteRepository();
        }


        public IGenericRepository<Pedido> GetPedidoRepository()
        {
            return new PedidoRepository();
        }


        public IGenericRepository<Mesa> GetMesaRepository()
        {
            return new MesaRepository();
        }

        public IGenericRepository<Direccion> GetDireccionesRepository()
        {
            return new DireccionRepository();
        }

        public IGenericRepository<Factura> GetFacturaRepository()
        {
            return new FacturaRepository();
        }

        public IGenericRepository<Factura_Pedido> GetFactura_PedidoRepository()
        {
            return new Factura_PedidoRepository();
        }

        public IGenericRepository<Ingrediente> GetIngredienteRepository()
        {
            return new IngredienteRepository();
        }

        public IGenericRepository<Menu> GetMenuRepository()
        {
            return new MenuRepository();
        }

        public IGenericRepository<Plato> GetPlatoRepository()
        {
            return new PlatoRepository();
        }

        public IGenericRepository<Plato_Ingrediente> GetPlato_IngredienteRepository()
        {
            return new Plato_IngredienteRepository();
        }

        public IGenericRepository<Plato_Pedido> GetPlato_PedidoRepository()
        {
            return new Plato_PedidoRepository();
        }

        public IGenericRepository<Plato_Precio> GetPlato_PrecioRepository()
        {
            return new Plato_PrecioRepository();
        }

        public IGenericRepository<Orden_Trabajo> GetOrden_TrabajoRepository()
        {
            return new Orden_TrabajoRepository();
        }

        public IGenericRepository<Stock> GetStockRepository()
        {
            return new StockRepository();
        }

        public IGenericRepository<Tipo_Transaccion_Stock> GetTipo_Transaccion_StockRepository()
        {
            return new Tipo_Transaccion_StockRepository();
        }

        public IGenericRepository<Transaccion_Stock> GetTransaccion_StockRepository()
        {
            return new Transaccion_StockRepository();
        }
    }
}
