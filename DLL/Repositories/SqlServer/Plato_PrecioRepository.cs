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
    class Plato_PrecioRepository : IGenericRepository<Plato_Precio>
    {


        #region Statements
        private string InsertStatement
        {
            get => "INSERT INTO [dbo].[PLATO_PRECIO] (Id_Empresa,Id_Sucursal,Id_Plato_Precio,Id_Plato,Fecha_Desde,Fecha_Hasta,Fecha_Create,Precio_Plato) VALUES (@Id_Empresa,@Id_Sucursal,@Id_Plato_Precio,@Id_Plato,@Fecha_Desde,@Fecha_Hasta,@Fecha_Create,@Precio_Plato)";
        }

        private string UpdateStatement
        {
            get => "UPDATE [dbo].[PLATO_PRECIO] SET Id_Plato_Precio=@Id_Plato_Precio,Id_Plato=@Id_Plato,Fecha_Desde=@Fecha_Desde,Fecha_Hasta=@Fecha_Hasta,Precio_Plato=@Precio_Plato WHERE  Id_Plato_Precio= @Id_Plato_Precio and Id_Empresa = @Id_Empresa and Id_Sucursal = @Id_Sucursal";
        }


        private string DeleteStatement
        {
            get => "DELETE FROM [dbo].[PLATO_PRECIO] WHERE Id_Plato_Precio = @Id_Plato_Precio and Id_Empresa = @Id_Empresa and Id_Sucursal = @Id_Sucursal";
        }

        private string SelectOneStatement
        {
            get => "SELECT Id_Empresa,Id_Sucursal,Id_Plato_Precio,Numero_Plato_Precio,Id_Plato,Fecha_Desde,Fecha_Hasta,Fecha_Create,Precio_Plato FROM [dbo].[PLATO_PRECIO] WHERE  Id_Plato_Precio= @Id_Plato_Precio and Id_Empresa = @Id_Empresa and Id_Sucursal = @Id_Sucursal";
        }

        private string SelectAllStatement
        {
            get => "SELECT Id_Empresa,Id_Sucursal,Id_Plato_Precio,Numero_Plato_Precio,Id_Plato,Fecha_Desde,Fecha_Hasta,Fecha_Create,Precio_Plato FROM [dbo].[PLATO_PRECIO] WHERE Id_Empresa = @Id_Empresa and Id_Sucursal = @Id_Sucursal";
        }
        #endregion

        public void Delete(Plato_Precio obj)
        {
            try
            {
                int y = SqlHelper.ExecuteNonQuery(DeleteStatement, System.Data.CommandType.Text,
                                                   new SqlParameter[] {
                                                   new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                                   new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                                   new SqlParameter("@Id_Plato_Precio", Guid.Parse(obj.Id_Plato_Precio.ToString())),});
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Plato_Precio - Error al eliminar un Plato_Precio de la base de datos por id pedido: {ex}", EventLevel.Error);
            }
        }

        public IEnumerable<Plato_Precio> GetAll(Plato_Precio obj)
        {
            List<Plato_Precio> plato_precios = new List<Plato_Precio>();
            try
            {
                LoggerManager.Current.Write("DAL Plato_Precio - Listando Plato_Precios", EventLevel.Informational);

                using (var dr = SqlHelper.ExecuteReader(SelectAllStatement, System.Data.CommandType.Text,
                                new SqlParameter[] {
                                new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString()))}))
                {
                    while (dr.Read())
                    {
                        Plato_Precio plato_precio = new Plato_Precio();

                        object[] values = new object[dr.FieldCount];
                        dr.GetValues(values);

                        plato_precio = Plato_PrecioAdapter.Current.Adapt(values);

                        plato_precios.Add(plato_precio);
                    }


                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Plato_Precio - Error al buscar Plato_Precios de la base de datos por id pedido: {ex}", EventLevel.Error);
            }
            return plato_precios;
        }

        public Plato_Precio GetOne(Plato_Precio obj)
        {
            Plato_Precio plato_precio = new Plato_Precio();

            LoggerManager.Current.Write("DAL Plato_Precio - Buscando un Plato_Precio", EventLevel.Informational);

            try
            {
                using (var dr = SqlHelper.ExecuteReader(SelectOneStatement, System.Data.CommandType.Text,
                                                        new SqlParameter[] {
                                                        new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                                        new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                                        new SqlParameter("@Id_Plato_Precio", Guid.Parse(obj.Id_Plato_Precio.ToString()))}))
                {
                    if (dr.Read())
                    {
                        //En este caso tendremos un solo registro...
                        object[] values = new object[dr.FieldCount];
                        dr.GetValues(values);

                        plato_precio = Plato_PrecioAdapter.Current.Adapt(values);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Plato_Precio - Error al buscar un Plato_Precio de la base de datos por id pedido: {ex}", EventLevel.Error);
            }
            return plato_precio;
        }

        public void Insert(Plato_Precio obj)
        {
            try
            {
                LoggerManager.Current.Write($"DAL Plato_Precio - Insertando el Id_Plato_Precio {Guid.Parse(obj.Id_Plato_Precio.ToString())} en Plato_Precios", EventLevel.Informational);
                int x = SqlHelper.ExecuteNonQuery(InsertStatement, System.Data.CommandType.Text,
                                                                       new SqlParameter[] {
                                              new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                              new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                              new SqlParameter("@Id_Plato_Precio", Guid.Parse(obj.Id_Plato_Precio.ToString())),
                                              //new SqlParameter("@Numero_Plato_Precio", obj.Numero_Plato_Precio),
                                              new SqlParameter("@Id_Plato", Guid.Parse(obj.Plato.Id_Plato.ToString())),
                                              new SqlParameter("@Fecha_Desde", obj.Fecha_Desde),
                                              new SqlParameter("@Fecha_Hasta", obj.Fecha_Hasta),
                                              new SqlParameter("@Fecha_Create", obj.Fecha_Create),
                                              new SqlParameter("@Precio_Plato", obj.Precio)});
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Plato_Precio - Error al insertar Plato_Precio de la base de datos por id pedido: {ex}", EventLevel.Error);
            }
        }

        public void Update(Plato_Precio obj)
        {
            try
            {
                LoggerManager.Current.Write($"DAL Plato_Precio - Actualizando el Id_Plato_Precio {Guid.Parse(obj.Id_Plato_Precio.ToString())} en Plato_Precios", EventLevel.Informational);
                int x = SqlHelper.ExecuteNonQuery(UpdateStatement, System.Data.CommandType.Text,
                                                                       new SqlParameter[] {
                                              new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                              new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                              new SqlParameter("@Id_Plato_Precio", Guid.Parse(obj.Id_Plato_Precio.ToString())),
                                              //new SqlParameter("@Numero_Plato_Precio", obj.Numero_Plato_Precio),
                                              new SqlParameter("@Id_Plato", Guid.Parse(obj.Plato.Id_Plato.ToString())),
                                              new SqlParameter("@Fecha_Desde", obj.Fecha_Desde),
                                              new SqlParameter("@Fecha_Hasta", obj.Fecha_Hasta),
                                              //new SqlParameter("@Fecha_Create", obj.Fecha_Create),
                                              new SqlParameter("@Precio_Plato", obj.Precio)});

            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Plato_Precio - Error al actualizar Plato_Precio de la base de datos por id pedido: {ex}", EventLevel.Error);
            }
        }
    }
}
