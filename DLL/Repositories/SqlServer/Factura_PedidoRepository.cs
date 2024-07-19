using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DAL.Contracts;
using DAL.Tools;
using DLL.Repositories.SqlServer.Adapters;
using Dominio;
using Servicios.Services;

namespace DLL.Repositories.SqlServer
{
    class Factura_PedidoRepository : IGenericRepository<Factura_Pedido>
    {


        #region Statements
        private string InsertStatement
        {
            get => "INSERT INTO [dbo].[Factura_Pedido] (Id_Empresa, Id_Sucursal, Id_Factura_Pedido,Id_Factura,Id_Pedido) VALUES (@Id_Empresa, @Id_Sucursal, @Id_Factura_Pedido,@Id_Factura,@Id_Pedido)";
        }

        private string UpdateStatement
        {
            get => "UPDATE [dbo].[Factura_Pedido] SET Id_Factura_Pedido=@Id_Factura_Pedido, Numero_Factura_Pedido=@Numero_Factura_Pedido,Id_Factura=@Id_Factura,Id_Pedido=@Id_Pedido WHERE  Id_Factura_Pedido= @Id_Factura_Pedido and Id_Empresa = @Id_Empresa and Id_Sucursal = @Id_Sucursal";
        }

        private string UpdateStatementEstado
        {
            get => "UPDATE [dbo].[Factura_Pedido] SET Estado=@Estado WHERE  Id_Factura_Pedido= @Id_Factura_Pedido and Id_Empresa = @Id_Empresa and Id_Sucursal = @Id_Sucursal";
        }

        private string DeleteStatement
        {
            get => "DELETE FROM [dbo].[Factura_Pedido] WHERE Id_Factura_Pedido = @Id_Factura_Pedido and Id_Empresa = @Id_Empresa and Id_Sucursal = @Id_Sucursal";
        }

        private string SelectOneStatement
        {
            get => "SELECT Id_Empresa, Id_Sucursal, Id_Factura_Pedido,Numero_Factura_Pedido,Id_Factura,Id_Pedido FROM [dbo].[Factura_Pedido] WHERE  Id_Factura_Pedido= @Id_Factura_Pedido and Id_Empresa = @Id_Empresa and Id_Sucursal = @Id_Sucursal";
        }

        private string SelectAllStatement
        {
            get => "SELECT Id_Empresa, Id_Sucursal, Id_Factura_Pedido,Numero_Factura_Pedido,Id_Factura,Id_Pedido FROM [dbo].[Factura_Pedido] where Id_Empresa = @Id_Empresa and Id_Sucursal = @Id_Sucursal";
        }
        #endregion

        public void Delete(Factura_Pedido obj)
        {
            try
            {
                LoggerManager.Current.Write("DAL Factura_Pedidos - Borrando Factura_Pedidos de la base de datos", EventLevel.Informational);

                int y = SqlHelper.ExecuteNonQuery(DeleteStatement, System.Data.CommandType.Text,
                                                   new SqlParameter[] {
                                                   new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                                   new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                                   new SqlParameter("@Id_Factura_Pedido", Guid.Parse(obj.Id_Factura_Pedido.ToString()))});
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Factura_Pedido - Error al borrar Factura_Pedido de la base de datos: {ex}", EventLevel.Error);
            }
        }

        public IEnumerable<Factura_Pedido> GetAll(Factura_Pedido obj)
        {
            List<Factura_Pedido> facturas_pedidos = new List<Factura_Pedido>();
            try
            {
                LoggerManager.Current.Write("DAL Factura_Pedidos - Buscando Factura_Pedidos de la base de datos", EventLevel.Informational);

                using (var dr = SqlHelper.ExecuteReader(SelectAllStatement, System.Data.CommandType.Text,
                                new SqlParameter[] {
                                new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString()))}))
                {
                    while (dr.Read())
                    {
                        Factura_Pedido factura_pedido = new Factura_Pedido();

                        object[] values = new object[dr.FieldCount];
                        dr.GetValues(values);

                        factura_pedido = Factura_PedidoAdapter.Current.Adapt(values);

                        facturas_pedidos.Add(factura_pedido);
                    }


                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Factura_Pedidos - Error al buscar Factura_Pedidos de la base de datos: {ex}", EventLevel.Error);
            }
            return facturas_pedidos;
        }

        public Factura_Pedido GetOne(Factura_Pedido obj)
        {
            Factura_Pedido factura_pedido = new Factura_Pedido();

            LoggerManager.Current.Write("DAL Factura_Pedidos - Buscando una Factura-Pedido en la Base de Datos", EventLevel.Informational);

            try
            {
                using (var dr = SqlHelper.ExecuteReader(SelectOneStatement, System.Data.CommandType.Text,
                                                        new SqlParameter[] {
                                                        new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                                        new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                                        new SqlParameter("@Id_Factura_Pedido", Guid.Parse(obj.Id_Factura_Pedido.ToString()))}))
                {
                    if (dr.Read())
                    {
                        //En este caso tendremos un solo registro...
                        object[] values = new object[dr.FieldCount];
                        dr.GetValues(values);

                        factura_pedido = Factura_PedidoAdapter.Current.Adapt(values);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Factura_Pedidos - Error al buscar una Factura_Pedidos de la base de datos: {ex}", EventLevel.Error);
            }
            return factura_pedido;
        }

        public void Insert(Factura_Pedido obj)
        {
            try
            {
                LoggerManager.Current.Write("DAL Factura_Pedidos - Insertando Factura_Pedidos de la base de datos", EventLevel.Informational);

                int x = SqlHelper.ExecuteNonQuery(InsertStatement, System.Data.CommandType.Text,
                                                                       new SqlParameter[] {
                                              new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                              new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                              new SqlParameter("@Id_Factura_Pedido", Guid.Parse(obj.Id_Factura_Pedido.ToString())),
                                              //new SqlParameter("@Numero_Factura_Pedido", obj.Numero_Factura_Pedido),
                                              new SqlParameter("@Id_Factura", Guid.Parse(obj.Factura.Id_Factura.ToString())),
                                              new SqlParameter("@Id_Pedido", Guid.Parse(obj.Pedido.Id_Pedido.ToString()))});
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Factura_Pedidos - Error al ingresar Factura_Pedidos de la base de datos: {ex}", EventLevel.Error);
            }
        }

        public void Update(Factura_Pedido obj)
        {
            try
            {
                LoggerManager.Current.Write("DAL Factura_Pedidos - Actualizando Factura_Pedidos de la base de datos", EventLevel.Informational);
                int x = SqlHelper.ExecuteNonQuery(UpdateStatement, System.Data.CommandType.Text,
                                                                       new SqlParameter[] {
                                              new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                              new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                              new SqlParameter("@Id_Factura_Pedido", Guid.Parse(obj.Id_Factura_Pedido.ToString())),
                                              new SqlParameter("@Numero_Factura_Pedido", obj.Numero_Factura_Pedido),
                                              new SqlParameter("@Id_Factura", Guid.Parse(obj.Factura.Id_Factura.ToString())),
                                              new SqlParameter("@Id_Pedido", Guid.Parse(obj.Pedido.Id_Pedido.ToString()))});

            }

            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Factura_Pedidos - Error al actualizar Factura_Pedidos de la base de datos: {ex}", EventLevel.Error);
            }
        }
    }
}
