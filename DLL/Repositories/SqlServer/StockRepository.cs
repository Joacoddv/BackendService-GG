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
    class StockRepository : IGenericRepository<Stock>
    {
        #region Statements
        private string InsertStatement
        {
            get => "INSERT INTO [dbo].[STOCK] (Id_Empresa,Id_Sucursal ,Id_Stock ,Numero_Stock ,Id_Ingrediente,Cantidad) VALUES (@Id_Empresa,Id_Sucursal ,Id_Stock ,Numero_Stock ,Id_Ingrediente,Cantidad)";
        }


        private string UpdateStatement
        {
            get => "UPDATE [dbo].[STOCK] SET Id_Ingrediente = @Id_Ingrediente ,Cantidad=@Cantidad WHERE  Id_Stock = @Id_Stock and Id_Empresa = @Id_Empresa and Id_Sucursal = @Id_Sucursal";
        }

        private string DeleteStatement
        {
            get => "DELETE FROM [dbo].[STOCK] WHERE Id_Stock = @Id_Stock and Id_Empresa = @Id_Empresa and Id_Sucursal = @Id_Sucursal";
        }

        private string SelectOneStatement
        {
            get => "SELECT Id_Empresa,Id_Sucursal ,Id_Stock ,Numero_Stock ,Id_Ingrediente,Cantidad FROM [dbo].[STOCK] WHERE Id_Stock = @Id_Stock and Id_Empresa = @Id_Empresa and Id_Sucursal = @Id_Sucursal";
        }

        private string SelectAllStatement
        {
            get => "SELECT Id_Empresa,Id_Sucursal ,Id_Stock ,Numero_Stock ,Id_Ingrediente,Cantidad FROM [dbo].[STOCK]  WHERE Id_Empresa = @Id_Empresa and Id_Sucursal=@Id_Sucursal";
        }
        #endregion

        public void Delete(Stock obj)
        {
            try
            {
                LoggerManager.Current.Write("DAL Stock - Eliminando Stock en la Base de Datos", EventLevel.Informational);
                int y = SqlHelper.ExecuteNonQuery(DeleteStatement, System.Data.CommandType.Text,
                                                   new SqlParameter[] {
                                                   new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                                   new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                                   new SqlParameter("@Id_Stock", Guid.Parse(obj.Id_Stock.ToString()))});

                LoggerManager.Current.Write("DAL Stock -  Stock eliminada en la base de datos con exito", EventLevel.Informational);
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Stock - Error al eliminar Stock de la base de datos: {ex}", EventLevel.Error);
            }
        }

        public IEnumerable<Stock> GetAll(Stock obj)
        {
            List<Stock> stocks = new List<Stock>();
            try
            {
                LoggerManager.Current.Write("DAL Stock - Buscando todas las Stock en la Base de Datos", EventLevel.Informational);
                using (var dr = SqlHelper.ExecuteReader(SelectAllStatement, System.Data.CommandType.Text,
                                new SqlParameter[] {
                                new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString()))}))
                {
                    while (dr.Read())
                    {
                        Stock stock = new Stock();

                        object[] values = new object[dr.FieldCount];
                        dr.GetValues(values);

                        stock = StockAdapter.Current.Adapt(values);
                        stocks.Add(stock);
                    }

                    LoggerManager.Current.Write("DAL Stock -  Stocks buscadas en la base de datos con exito", EventLevel.Informational);
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Stock - Error al buscar Stocks de la base de datos: {ex}", EventLevel.Error);
            }
            return stocks;
        }

        public Stock GetOne(Stock obj)
        {
            Stock stock = new Stock();

            LoggerManager.Current.Write("DAL Stock - Buscando una Stock en la Base de Datos", EventLevel.Informational);

            try
            {
                using (var dr = SqlHelper.ExecuteReader(SelectOneStatement, System.Data.CommandType.Text,
                                new SqlParameter[] {
                                new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                new SqlParameter("@Id_Stock", Guid.Parse(obj.Id_Stock.ToString()))}))
                {
                    if (dr.Read())
                    {
                        //En este caso tendremos un solo registro...
                        object[] values = new object[dr.FieldCount];
                        dr.GetValues(values);

                        stock = StockAdapter.Current.Adapt(values);
                    }
                }
                LoggerManager.Current.Write("DAL Stock -  Stock buscaada en la base de datos con exito", EventLevel.Informational);
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Stock - Error al buscar una Stock de la base de datos: {ex}", EventLevel.Error);
            }
            return stock;
        }

        public void Insert(Stock obj)
        {
            LoggerManager.Current.Write("DAL Stock - Insertando Stock en la Base de Datos", EventLevel.Informational);
            try
            {
                int x = SqlHelper.ExecuteNonQuery(InsertStatement,
                                                   System.Data.CommandType.Text,
                                                   new SqlParameter[] {
                                              new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                              new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                              new SqlParameter("@Id_Stock", Guid.Parse(obj.Id_Stock.ToString())),
                                              new SqlParameter("@Numero_Stock", obj.Numero_Stock),
                                              new SqlParameter("@Id_Ingrediente", obj.Ingrediente.Id_Ingrediente),
                                              new SqlParameter("@Cantidad", obj.Cantidad)});

                LoggerManager.Current.Write("DAL Stock -  Stock insertada en la base de datos con exito", EventLevel.Informational);
            }

            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Stock - Error al insertar Stock de la base de datos: {ex}", EventLevel.Error);
            }
        }


        public void Update(Stock obj)
        {
            LoggerManager.Current.Write("DAL Stock - Actualizando Stock en la Base de Datos", EventLevel.Informational);
            try
            {
                int x = SqlHelper.ExecuteNonQuery(UpdateStatement,
                                                   System.Data.CommandType.Text,
                                                   new SqlParameter[] {
                                              new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                              new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                              new SqlParameter("@Id_Stock", Guid.Parse(obj.Id_Stock.ToString())),
                                              new SqlParameter("@Numero_Stock", obj.Numero_Stock),
                                              new SqlParameter("@Id_Ingrediente", obj.Ingrediente.Id_Ingrediente),
                                              new SqlParameter("@Cantidad", obj.Cantidad)});

                LoggerManager.Current.Write("DAL Stock -  Stock actualizada en la base de datos con exito", EventLevel.Informational);
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Stock - Error al actualizar Stock de la base de datos: {ex}", EventLevel.Error);
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
