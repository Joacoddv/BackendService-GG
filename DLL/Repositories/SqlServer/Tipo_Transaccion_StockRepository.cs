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
    class Tipo_Transaccion_StockRepository : IGenericRepository<Tipo_Transaccion_Stock>
    {
        #region Statements
        private string InsertStatement
        {
            get => "INSERT INTO [dbo].[ORDEN_TRABAJO] (Id_Empresa,Id_Sucursal ,Id_Tipo_Transaccion_Stock ,Numero_Tipo_Transaccion_Stock ,Descripcion_Tipo_Transaccion_Stock) VALUES (@Id_Empresa,@Id_Sucursal ,@Id_Tipo_Transaccion_Stock ,@Numero_Tipo_Transaccion_Stock ,@Descripcion_Tipo_Transaccion_Stock)";
        }


        private string UpdateStatement
        {
            get => "UPDATE [dbo].[ORDEN_TRABAJO] SET Descripcion_Tipo_Transaccion_Stock = @Descripcion_Tipo_Transaccion_Stock WHERE  Id_Tipo_Transaccion_Stock = @Id_Tipo_Transaccion_Stock and Id_Empresa = @Id_Empresa and Id_Sucursal = @Id_Sucursal";
        }

        private string DeleteStatement
        {
            get => "DELETE FROM [dbo].[ORDEN_TRABAJO] WHERE Id_Tipo_Transaccion_Stock = @Id_Tipo_Transaccion_Stock and Id_Empresa = @Id_Empresa and Id_Sucursal = @Id_Sucursal";
        }

        private string SelectOneStatement
        {
            get => "SELECT Id_Empresa,Id_Sucursal ,Id_Tipo_Transaccion_Stock ,Numero_Tipo_Transaccion_Stock ,Descripcion_Tipo_Transaccion_Stock FROM [dbo].[ORDEN_TRABAJO] WHERE Id_Tipo_Transaccion_Stock = @Id_Tipo_Transaccion_Stock and Id_Empresa = @Id_Empresa and Id_Sucursal = @Id_Sucursal";
        }

        private string SelectAllStatement
        {
            get => "SELECT Id_Empresa,Id_Sucursal ,Id_Tipo_Transaccion_Stock ,Numero_Tipo_Transaccion_Stock ,Descripcion_Tipo_Transaccion_Stock FROM [dbo].[ORDEN_TRABAJO]  WHERE Id_Empresa = @Id_Empresa and Id_Sucursal=@Id_Sucursal";
        }
        #endregion

        public void Delete(Tipo_Transaccion_Stock obj)
        {
            try
            {
                LoggerManager.Current.Write("DAL Tipo_Transaccion_Stock - Eliminando Tipo_Transaccion_Stock en la Base de Datos", EventLevel.Informational);
                int y = SqlHelper.ExecuteNonQuery(DeleteStatement, System.Data.CommandType.Text,
                                                   new SqlParameter[] {
                                                   new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                                   new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                                   new SqlParameter("@Id_Tipo_Transaccion_Stock", Guid.Parse(obj.Id_Tipo_Transaccion_Stock.ToString()))});

                LoggerManager.Current.Write("DAL Tipo_Transaccion_Stock -  Tipo_Transaccion_Stock eliminada en la base de datos con exito", EventLevel.Informational);
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Tipo_Transaccion_Stock - Error al eliminar Tipo_Transaccion_Stock de la base de datos: {ex}", EventLevel.Error);
            }
        }

        public IEnumerable<Tipo_Transaccion_Stock> GetAll(Tipo_Transaccion_Stock obj)
        {
            List<Tipo_Transaccion_Stock> tipo_transaccion_stocks = new List<Tipo_Transaccion_Stock>();
            try
            {
                LoggerManager.Current.Write("DAL Tipo_Transaccion_Stock - Buscando todas las Tipo_Transaccion_Stock en la Base de Datos", EventLevel.Informational);
                using (var dr = SqlHelper.ExecuteReader(SelectAllStatement, System.Data.CommandType.Text,
                                new SqlParameter[] {
                                new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString()))}))
                {
                    while (dr.Read())
                    {
                        Tipo_Transaccion_Stock tipo_transaccion_stock = new Tipo_Transaccion_Stock();

                        object[] values = new object[dr.FieldCount];
                        dr.GetValues(values);

                        tipo_transaccion_stock = Tipo_Transaccion_StockAdapter.Current.Adapt(values);
                        tipo_transaccion_stocks.Add(tipo_transaccion_stock);
                    }

                    LoggerManager.Current.Write("DAL Tipo_Transaccion_Stock -  Tipo_Transacciones_Stock buscadas en la base de datos con exito", EventLevel.Informational);
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Tipo_Transaccion_Stock - Error al buscar Tipo_Transaccion_Stock de la base de datos: {ex}", EventLevel.Error);
            }
            return tipo_transaccion_stocks;
        }

        public Tipo_Transaccion_Stock GetOne(Tipo_Transaccion_Stock obj)
        {
            Tipo_Transaccion_Stock tipo_transaccion_stock = new Tipo_Transaccion_Stock();

            LoggerManager.Current.Write("DAL Tipo_Transaccion_Stock - Buscando una Tipo_Transaccion_Stock en la Base de Datos", EventLevel.Informational);

            try
            {
                using (var dr = SqlHelper.ExecuteReader(SelectOneStatement, System.Data.CommandType.Text,
                                new SqlParameter[] {
                                new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                new SqlParameter("@Id_Tipo_Transaccion_Stock", Guid.Parse(obj.Id_Tipo_Transaccion_Stock.ToString()))}))
                {
                    if (dr.Read())
                    {
                        //En este caso tendremos un solo registro...
                        object[] values = new object[dr.FieldCount];
                        dr.GetValues(values);

                        tipo_transaccion_stock = Tipo_Transaccion_StockAdapter.Current.Adapt(values);
                    }
                }
                LoggerManager.Current.Write("DAL Tipo_Transaccion_Stock -  Tipo_Transaccion_Stock buscaada en la base de datos con exito", EventLevel.Informational);
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Tipo_Transaccion_Stock - Error al buscar una Tipo_Transaccion_Stock de la base de datos: {ex}", EventLevel.Error);
            }
            return tipo_transaccion_stock;
        }

        public void Insert(Tipo_Transaccion_Stock obj)
        {
            LoggerManager.Current.Write("DAL Tipo_Transaccion_Stock - Insertando Tipo_Transaccion_Stock en la Base de Datos", EventLevel.Informational);
            try
            {
                int x = SqlHelper.ExecuteNonQuery(InsertStatement,
                                                   System.Data.CommandType.Text,
                                                   new SqlParameter[] {
                                              new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                              new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                              new SqlParameter("@Id_Tipo_Transaccion_Stock", Guid.Parse(obj.Id_Tipo_Transaccion_Stock.ToString())),
                                              new SqlParameter("@Numero_Tipo_Transaccion_Stock", obj.Numero_Tipo_Transaccion_Stock),
                                              new SqlParameter("@Descripcion_Tipo_Transaccion_Stock", obj.Descripcion_Tipo_Transaccion_Stock)});

                LoggerManager.Current.Write("DAL Tipo_Transaccion_Stock -  Tipo_Transaccion_Stock insertada en la base de datos con exito", EventLevel.Informational);
            }

            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Tipo_Transaccion_Stock - Error al insertar Tipo_Transaccion_Stock de la base de datos: {ex}", EventLevel.Error);
            }
        }


        public void Update(Tipo_Transaccion_Stock obj)
        {
            LoggerManager.Current.Write("DAL Tipo_Transaccion_Stock - Actualizando Tipo_Transaccion_Stock en la Base de Datos", EventLevel.Informational);
            try
            {
                int x = SqlHelper.ExecuteNonQuery(UpdateStatement,
                                                   System.Data.CommandType.Text,
                                                   new SqlParameter[] {
                                              new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                              new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                              new SqlParameter("@Id_Tipo_Transaccion_Stock", Guid.Parse(obj.Id_Tipo_Transaccion_Stock.ToString())),
                                              new SqlParameter("@Numero_Tipo_Transaccion_Stock", obj.Numero_Tipo_Transaccion_Stock),
                                              new SqlParameter("@Descripcion_Tipo_Transaccion_Stock", obj.Descripcion_Tipo_Transaccion_Stock)});

                LoggerManager.Current.Write("DAL Tipo_Transaccion_Stock -  Tipo_Transaccion_Stock actualizada en la base de datos con exito", EventLevel.Informational);
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Tipo_Transaccion_Stock - Error al actualizar Tipo_Transaccion_Stock de la base de datos: {ex}", EventLevel.Error);
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
