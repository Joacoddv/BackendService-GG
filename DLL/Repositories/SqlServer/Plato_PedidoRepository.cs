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
    public class Plato_PedidoRepository : IGenericRepository<Plato_Pedido>
    {


        #region Statements
        private string InsertStatement
        {
            get => "INSERT INTO [dbo].[PLATO_PEDIDO] (Id_Empresa,Id_Sucursal,Id_Plato_Pedido,Id_Pedido,Id_Plato,Cantidad,Observaciones) VALUES (@Id_Empresa,@Id_Sucursal,@Id_Plato_Pedido,@Id_Pedido,@Id_Plato,@Cantidad,@Observaciones)";
        }

        private string UpdateStatement
        {
            get => "UPDATE [dbo].[PLATO_PEDIDO] SET Id_Plato_Pedido=@Id_Plato_Pedido,Id_Pedido=@Id_Pedido,Id_Plato=@Id_Plato,Cantidad=@Cantidad,Observaciones=@Observaciones WHERE  Id_Plato_Pedido= @Id_Plato_Pedido and Id_Empresa = @Id_Empresa and Id_Sucursal =@Id_Sucursal";
        }

        private string UpdateStatementEstado
        {
            get => "UPDATE [dbo].[PLATO_PEDIDO] SET Estado=@Estado WHERE  Id_Plato_Pedido= @Id_Plato_Pedido and Id_Empresa = @Id_Empresa and Id_Sucursal =@Id_Sucursal";
        }

        private string DeleteStatement
        {
            get => "DELETE FROM [dbo].[PLATO_PEDIDO] WHERE Id_Plato_Pedido = @Id_Plato_Pedido and Id_Empresa = @Id_Empresa and Id_Sucursal =@Id_Sucursal";
        }

        private string SelectOneStatement
        {
            get => "SELECT Id_Empresa,Id_Sucursal,Id_Plato_Pedido,Numero_Plato_Pedido,Id_Pedido,Id_Plato,Cantidad,Observaciones FROM [dbo].[PLATO_PEDIDO] WHERE  Id_Plato_Pedido= @Id_Plato_Pedido and Id_Empresa = @Id_Empresa and Id_Sucursal =@Id_Sucursal";
        }

        private string SelectOneStatementIdPedido
        {
            get => "SELECT Id_Empresa,Id_Sucursal,Id_Plato_Pedido,Numero_Plato_Pedido,Id_Pedido,Id_Plato,Cantidad,Observaciones FROM [dbo].[PLATO_PEDIDO] WHERE  Id_Pedido= @Id_Pedido and Id_Empresa = @Id_Empresa and Id_Sucursal =@Id_Sucursal";
        }

        private string SelectAllStatement
        {
            get => "SELECT Id_Empresa,Id_Sucursal,Id_Plato_Pedido,Numero_Plato_Pedido,Id_Pedido,Id_Plato,Cantidad,Observaciones FROM [dbo].[PLATO_PEDIDO] WHERE Id_Empresa = @Id_Empresa and Id_Sucursal =@Id_Sucursal";
        }
        #endregion

        public void Delete(Plato_Pedido obj)
        {
            try
            {
                int y = SqlHelper.ExecuteNonQuery(DeleteStatement, System.Data.CommandType.Text,
                                                   new SqlParameter[] {
                                                   new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                                   new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                                   new SqlParameter("@Id_Plato_Pedido", Guid.Parse(obj.Id_Plato_Pedido.ToString()))});
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Plato_Pedido - Error al elminar Plato_Pedido de la base de datos: {ex}", EventLevel.Error);
            }
        }

        public IEnumerable<Plato_Pedido> GetAll(Plato_Pedido obj)
        {
            List<Plato_Pedido> plato_pedidos = new List<Plato_Pedido>();
            try
            {
                using (var dr = SqlHelper.ExecuteReader(SelectAllStatement, System.Data.CommandType.Text,
                                new SqlParameter[] {
                                new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString()))
                                }))
                {
                    while (dr.Read())
                    {
                        Plato_Pedido plato_pedido = new Plato_Pedido();

                        object[] values = new object[dr.FieldCount];
                        dr.GetValues(values);

                        plato_pedido = Plato_PedidoAdapter.Current.Adapt(values);

                        plato_pedidos.Add(plato_pedido);
                    }


                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Plato_Pedido - Error al buscar Plato_Pedidos de la base de datos: {ex}", EventLevel.Error);
            }
            return plato_pedidos;
        }

        public Plato_Pedido GetOne(Plato_Pedido obj)
        {
            Plato_Pedido plato_pedido = new Plato_Pedido();

            LoggerManager.Current.Write("Buscando Plato_Pedido en la Base de Datos", EventLevel.Informational);

            try
            {
                using (var dr = SqlHelper.ExecuteReader(SelectOneStatement, System.Data.CommandType.Text,
                                                        new SqlParameter[] { 
                                                        new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                                        new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                                        new SqlParameter("@Id_Plato_Pedido", Guid.Parse(obj.Id_Plato_Pedido.ToString()))}))
                {
                    if (dr.Read())
                    {
                        //En este caso tendremos un solo registro...
                        object[] values = new object[dr.FieldCount];
                        dr.GetValues(values);

                        plato_pedido = Plato_PedidoAdapter.Current.Adapt(values);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Plato_Pedido - Error al buscar un Plato_Pedido de la base de datos: {ex}", EventLevel.Error);
            }
            return plato_pedido;
        }

        public void Insert(Plato_Pedido obj)
        {
            try
            {
                int x = SqlHelper.ExecuteNonQuery(InsertStatement, System.Data.CommandType.Text,
                                                                       new SqlParameter[] {
                                              new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                              new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                              new SqlParameter("@Id_Plato_Pedido", Guid.Parse(obj.Id_Plato_Pedido.ToString())),
                                              //new SqlParameter("@Numero_Plato_Pedido", obj.Numero_Plato_Pedido),
                                              new SqlParameter("@Id_Pedido", Guid.Parse(obj.Pedido.Id_Pedido.ToString())),
                                              new SqlParameter("@Id_Plato", Guid.Parse(obj.Plato.Id_Plato.ToString())),
                                              new SqlParameter("@Cantidad", obj.Cantidad),
                                              new SqlParameter("@Observaciones", obj.Observaciones)});
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Plato_Pedido - Error al insertar Plato_Pedido de la base de datos: {ex}", EventLevel.Error);
            }
        }

        public void Update(Plato_Pedido obj)
        {
            try
            {
                int x = DAL.Tools.SqlHelper.ExecuteNonQuery(UpdateStatement, System.Data.CommandType.Text,
                                                                       new SqlParameter[] {
                                              new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                              new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                              new SqlParameter("@Id_Plato_Pedido", Guid.Parse(obj.Id_Plato_Pedido.ToString())),
                                              //new SqlParameter("@Numero_Plato_Pedido", obj.Numero_Plato_Pedido),
                                              new SqlParameter("@Id_Pedido", Guid.Parse(obj.Pedido.Id_Pedido.ToString())),
                                              new SqlParameter("@Id_Plato", Guid.Parse(obj.Plato.Id_Plato.ToString())),
                                              new SqlParameter("@Cantidad", obj.Cantidad),
                                              new SqlParameter("@Observaciones", obj.Observaciones)});

            }

            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Plato_Pedido - Error al actualizar Plato_Pedido de la base de datos: {ex}", EventLevel.Error);
            }
        }


        public IEnumerable<Plato_Pedido> GetOnePedido(Plato_Pedido obj)
        {
            List<Plato_Pedido> plato_pedidos = new List<Plato_Pedido>();
            try
            {
                using (var dr = SqlHelper.ExecuteReader(SelectOneStatementIdPedido, System.Data.CommandType.Text,
                                new SqlParameter[] {
                                new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                new SqlParameter("@Id_Plato_Pedido", Guid.Parse(obj.Id_Plato_Pedido.ToString())),
                                new SqlParameter("@Id_Pedido", Guid.Parse(obj.Pedido.Id_Pedido.ToString())),}))
                {
                    while (dr.Read())
                    {
                        Plato_Pedido plato_pedido = new Plato_Pedido();

                        object[] values = new object[dr.FieldCount];
                        dr.GetValues(values);

                        plato_pedido = Plato_PedidoAdapter.Current.Adapt(values);

                        plato_pedidos.Add(plato_pedido);
                    }


                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Plato_Pedido - Error al buscar un Plato_Pedido de la base de datos por id pedido: {ex}", EventLevel.Error);
            }
            return plato_pedidos;
        }
    }
}
