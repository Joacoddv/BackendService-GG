using DAL.Contracts;
using DAL.Tools;
using DLL.Repositories.SqlServer.Adapters;
using Dominio;
using Servicios.Services;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Diagnostics.Tracing;

namespace DLL.Repositories.SqlServer
{
    class Transaccion_StockRepository : IGenericRepository<Transaccion_Stock>
    {
        #region Statements
        private string InsertStatement
        {
            get => "INSERT INTO [dbo].[TRANSACCION_STOCK] (Id_Empresa,Id_Sucursal ,Id_Transaccion_Stock ,Id_Tipo_Transaccion_Stock ,Fecha_Transaccion,Id_OT,Id_Ingrediente,Cantidad,Cantidad_Restante) VALUES (@Id_Empresa,@Id_Sucursal ,@Id_Transaccion_Stock ,@Id_Tipo_Transaccion_Stock ,@Fecha_Transaccion,@Id_OT,@Id_Ingrediente,@Cantidad,@Cantidad_Restante)";
        }


        private string UpdateStatement
        {
            get => "UPDATE [dbo].[TRANSACCION_STOCK] SET Id_Tipo_Transaccion_Stock=@Id_Tipo_Transaccion_Stock ,Fecha_Transaccion=@Fecha_Transaccion,Id_OT=@Id_OT,Id_Ingrediente=@Id_Ingrediente,Cantidad=@Cantidad,Cantidad_Restante=@Cantidad_Restante WHERE  Id_Transaccion_Stock = @Id_Transaccion_Stock and Id_Empresa = @Id_Empresa and Id_Sucursal = @Id_Sucursal";
        }

        private string DeleteStatement
        {
            get => "DELETE FROM [dbo].[TRANSACCION_STOCK] WHERE Id_Transaccion_Stock = @Id_Transaccion_Stock and Id_Empresa = @Id_Empresa and Id_Sucursal = @Id_Sucursal";
        }

        private string SelectOneStatement
        {
            get => "SELECT Id_Empresa,Id_Sucursal ,Id_Transaccion_Stock ,Id_Tipo_Transaccion_Stock ,Fecha_Transaccion,Id_OT,Id_Ingrediente,Cantidad,Cantidad_Restante FROM [dbo].[TRANSACCION_STOCK] WHERE Id_Transaccion_Stock = @Id_Transaccion_Stock and Id_Empresa = @Id_Empresa and Id_Sucursal = @Id_Sucursal";
        }

        private string SelectAllStatement
        {
            get => "SELECT Id_Empresa,Id_Sucursal ,Id_Transaccion_Stock ,Id_Tipo_Transaccion_Stock ,Fecha_Transaccion,Id_OT,Id_Ingrediente,Cantidad,Cantidad_Restante FROM [dbo].[TRANSACCION_STOCK]  WHERE Id_Empresa = @Id_Empresa and Id_Sucursal=@Id_Sucursal";
        }
        #endregion

        public void Delete(Transaccion_Stock obj)
        {
            try
            {
                LoggerManager.Current.Write("DAL Transaccion_Stock - Eliminando Transaccion_Stock en la Base de Datos", EventLevel.Informational);
                int y = SqlHelper.ExecuteNonQuery(DeleteStatement, System.Data.CommandType.Text,
                                                   new SqlParameter[] {
                                                   new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                                   new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                                   new SqlParameter("@Id_Transaccion_Stock", Guid.Parse(obj.Id_Transaccion_Stock.ToString()))});

                LoggerManager.Current.Write("DAL Transaccion_Stock -  Transaccion_Stock eliminada en la base de datos con exito", EventLevel.Informational);
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Transaccion_Stock - Error al eliminar Transaccion_Stock de la base de datos: {ex}", EventLevel.Error);
            }
        }

        public IEnumerable<Transaccion_Stock> GetAll(Transaccion_Stock obj)
        {
            List<Transaccion_Stock> transacciones_stock = new List<Transaccion_Stock>();
            try
            {
                LoggerManager.Current.Write("DAL Transaccion_Stock - Buscando todas las Transaccion_Stock en la Base de Datos", EventLevel.Informational);
                using (var dr = SqlHelper.ExecuteReader(SelectAllStatement, System.Data.CommandType.Text,
                                new SqlParameter[] {
                                new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString()))}))
                {
                    while (dr.Read())
                    {
                        Transaccion_Stock transaccion_stock = new Transaccion_Stock();

                        object[] values = new object[dr.FieldCount];
                        dr.GetValues(values);

                        transaccion_stock = Transaccion_StockAdapter.Current.Adapt(values);
                        transacciones_stock.Add(transaccion_stock);
                    }

                    LoggerManager.Current.Write("DAL Transaccion_Stock -  Transacciones_Stock buscadas en la base de datos con exito", EventLevel.Informational);
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Transaccion_Stock - Error al buscar Transaccion_Stock de la base de datos: {ex}", EventLevel.Error);
            }
            return transacciones_stock;
        }

        public Transaccion_Stock GetOne(Transaccion_Stock obj)
        {
            Transaccion_Stock transaccion_stock = new Transaccion_Stock();

            LoggerManager.Current.Write("DAL Transaccion_Stock - Buscando una Transaccion_Stock en la Base de Datos", EventLevel.Informational);

            try
            {
                using (var dr = SqlHelper.ExecuteReader(SelectOneStatement, System.Data.CommandType.Text,
                                new SqlParameter[] {
                                new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                new SqlParameter("@Id_Transaccion_Stock", Guid.Parse(obj.Id_Transaccion_Stock.ToString()))}))
                {
                    if (dr.Read())
                    {
                        //En este caso tendremos un solo registro...
                        object[] values = new object[dr.FieldCount];
                        dr.GetValues(values);

                        transaccion_stock = Transaccion_StockAdapter.Current.Adapt(values);
                    }
                }
                LoggerManager.Current.Write("DAL Transaccion_Stock -  Transaccion_Stock buscaada en la base de datos con exito", EventLevel.Informational);
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Transaccion_Stock - Error al buscar una Transaccion_Stock de la base de datos: {ex}", EventLevel.Error);
            }
            return transaccion_stock;
        }

        public void Insert(Transaccion_Stock obj)
        {
            LoggerManager.Current.Write("DAL Transaccion_Stock - Insertando Transaccion_Stock en la Base de Datos", EventLevel.Informational);
            try
            {
                int x = SqlHelper.ExecuteNonQuery(InsertStatement,
                                                   System.Data.CommandType.Text,
                                                   new SqlParameter[] {
                                              new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                              new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                              new SqlParameter("@Id_Transaccion_Stock", Guid.Parse(obj.Id_Transaccion_Stock.ToString())),
                                              new SqlParameter("@Id_Tipo_Transaccion_Stock", Guid.Parse(obj.Tipo_Transaccion_Stock.Id_Tipo_Transaccion_Stock.ToString())),
                                              new SqlParameter("@Fecha_Transaccion", obj.Fecha_Transaccion),
                                              new SqlParameter("@Id_OT", Guid.Parse(obj.Orden_Trabajo.Id_Orden_Trabajo.ToString())),
                                              new SqlParameter("@Id_Ingrediente", Guid.Parse(obj.Ingrediente.Id_Ingrediente.ToString())),
                                              new SqlParameter("@Cantidad", obj.Cantidad),
                                              new SqlParameter("@Cantidad_Restante", obj.Cantidad_Restante)});

                LoggerManager.Current.Write("DAL Transaccion_Stock -  Transaccion_Stock insertada en la base de datos con exito", EventLevel.Informational);
            }

            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Transaccion_Stock - Error al insertar Transaccion_Stock de la base de datos: {ex}", EventLevel.Error);
            }
        }


        public void Update(Transaccion_Stock obj)
        {
            LoggerManager.Current.Write("DAL Transaccion_Stock - Actualizando Transaccion_Stock en la Base de Datos", EventLevel.Informational);
            try
            {
                int x = SqlHelper.ExecuteNonQuery(UpdateStatement,
                                                   System.Data.CommandType.Text,
                                                   new SqlParameter[] {
                                              new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                              new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                              new SqlParameter("@Id_Transaccion_Stock", Guid.Parse(obj.Id_Transaccion_Stock.ToString())),
                                              new SqlParameter("@Id_Tipo_Transaccion_Stock", Guid.Parse(obj.Tipo_Transaccion_Stock.Id_Tipo_Transaccion_Stock.ToString())),
                                              new SqlParameter("@Fecha_Transaccion", obj.Fecha_Transaccion),
                                              new SqlParameter("@Id_OT", Guid.Parse(obj.Orden_Trabajo.Id_Orden_Trabajo.ToString())),
                                              new SqlParameter("@Id_Ingrediente", Guid.Parse(obj.Ingrediente.Id_Ingrediente.ToString())),
                                              new SqlParameter("@Cantidad", obj.Cantidad),
                                              new SqlParameter("@Cantidad_Restante", obj.Cantidad_Restante)});

                LoggerManager.Current.Write("DAL Transaccion_Stock -  Transaccion_Stock actualizada en la base de datos con exito", EventLevel.Informational);
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Transaccion_Stock - Error al actualizar Transaccion_Stock de la base de datos: {ex}", EventLevel.Error);
                //throw new Exception(ex.Message);
            }
        }


        private object ValidarNull(object obj)
        {
            if (obj == null) //|| Convert.ToInt32(obj.ToString()) == 0)
            {
                return DBNull.Value;
            }
            else
            {
                return obj;
            }
        }
    }
}
