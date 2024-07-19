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
    class MesaRepository : IGenericRepository<Mesa>
    {
        #region Statements
        private string InsertStatement
        {
            get => "INSERT INTO [dbo].[Mesa] (Id_Empresa,Id_Sucursal ,Id_Mesa ,Ubicación_Mesa,Cantidad) VALUES (@Id_Empresa,@Id_Sucursal ,@Id_Mesa  ,@Ubicación_Mesa,@Cantidad)";
        }


        private string UpdateStatement
        {
            get => "UPDATE [dbo].[Mesa] SET Ubicación_Mesa=@Ubicación_Mesa,Cantidad=@Cantidad WHERE  Id_Mesa = @Id_mesa and Id_Empresa = @Id_Empresa and Id_Sucursal = @Id_Sucursal";
        }

        private string DeleteStatement
        {
            get => "DELETE FROM [dbo].[Mesa] WHERE Id_Mesa = @Id_mesa and Id_Empresa = @Id_Empresa and Id_Sucursal = @Id_Sucursal";
        }

        private string SelectOneStatement
        {
            get => "SELECT Id_Empresa,Id_Sucursal ,Id_Mesa ,Numero_Mesa ,Ubicación_Mesa,Cantidad FROM [dbo].[Mesa] WHERE Id_Mesa = @Id_Mesa and Id_Empresa = @Id_Empresa and Id_Sucursal = @Id_Sucursal";
        }

        private string SelectAllStatement
        {
            get => "SELECT Id_Empresa,Id_Sucursal ,Id_Mesa ,Numero_Mesa ,Ubicación_Mesa,Cantidad FROM [dbo].[Mesa]  WHERE Id_Empresa = @Id_Empresa and Id_Sucursal=@Id_Sucursal";
        }
        #endregion

        public void Delete(Mesa obj)
        {
            try
            {
                LoggerManager.Current.Write("DAL Mesa - Eliminando mesa en la Base de Datos", EventLevel.Informational);
                int y = SqlHelper.ExecuteNonQuery(DeleteStatement, System.Data.CommandType.Text,
                                                   new SqlParameter[] {
                                                   new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                                   new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                                   new SqlParameter("@Id_Mesa", Guid.Parse(obj.Id_Mesa.ToString()))});
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Mesa - Error al eliminar mesa de la base de datos: {ex}", EventLevel.Error);
            }
        }

        public IEnumerable<Mesa> GetAll(Mesa obj)
        {
            List<Mesa> mesas = new List<Mesa>();
            try
            {
                LoggerManager.Current.Write("DAL Mesa - Buscando todas las mesas en la Base de Datos", EventLevel.Informational);
                using (var dr = SqlHelper.ExecuteReader(SelectAllStatement, System.Data.CommandType.Text,
                                new SqlParameter[] {
                                new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString()))}))
                {
                    while (dr.Read())
                    {
                        Mesa mesa = new Mesa();

                        object[] values = new object[dr.FieldCount];
                        dr.GetValues(values);

                        mesa = MesaAdapter.Current.Adapt(values);
                        mesas.Add(mesa);
                    }


                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Mesa - Error al buscar mesas de la base de datos: {ex}", EventLevel.Error);
            }
            return mesas;
        }

        public Mesa GetOne(Mesa obj)
        {
            Mesa mesa = new Mesa();

            LoggerManager.Current.Write("DAL Mesa - Buscando una mesa en la Base de Datos", EventLevel.Informational);

            try
            {
                using (var dr = SqlHelper.ExecuteReader(SelectOneStatement, System.Data.CommandType.Text,
                                new SqlParameter[] {
                                new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                new SqlParameter("@Id_Mesa", Guid.Parse(obj.Id_Mesa.ToString()))}))
                {
                    if (dr.Read())
                    {
                        //En este caso tendremos un solo registro...
                        object[] values = new object[dr.FieldCount];
                        dr.GetValues(values);

                        mesa = MesaAdapter.Current.Adapt(values);
                    }
                }
                LoggerManager.Current.Write("DAL Mesa - Mesa buscada en la base de datos con exito", EventLevel.Informational);
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Mesa - Error al buscar una mesa de la base de datos: {ex}", EventLevel.Error);
            }
            return mesa;
        }

        public void Insert(Mesa obj)
        {
            LoggerManager.Current.Write("DAL Mesa - Insertando mesa en la Base de Datos", EventLevel.Informational);
            try
            {
                int x = SqlHelper.ExecuteNonQuery(InsertStatement,
                                                   System.Data.CommandType.Text,
                                                   new SqlParameter[] {
                                              new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                              new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                              new SqlParameter("@Id_Mesa", Guid.Parse(obj.Id_Mesa.ToString())),
                                             // new SqlParameter("@Numero_Mesa", obj.Numero_Mesa),
                                              new SqlParameter("@Ubicación_Mesa", obj.Ubicacion_Mesa),
                                              new SqlParameter("@Cantidad", obj.Cantidad)});

                LoggerManager.Current.Write("DAL Mesa - Mesa insertada en la base de datos con exito", EventLevel.Informational);
            }

            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Mesa - Error al insertar mesa de la base de datos: {ex}", EventLevel.Error);
            }
        }


        public void Update(Mesa obj)
        {
            LoggerManager.Current.Write("DAL Mesa - Actualizando mesa en la Base de Datos", EventLevel.Informational);
            try
            {
                int x = SqlHelper.ExecuteNonQuery(UpdateStatement,
                                                   System.Data.CommandType.Text,
                                                   new SqlParameter[] {
                                              new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                              new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                              new SqlParameter("@Id_Mesa", Guid.Parse(obj.Id_Mesa.ToString())),
                                              new SqlParameter("@Numero_Mesa", obj.Numero_Mesa),
                                              new SqlParameter("@Ubicación_Mesa", obj.Ubicacion_Mesa),
                                              new SqlParameter("@Cantidad", obj.Cantidad)});

                LoggerManager.Current.Write("DAL Mesa - Mesa actualziada en la base de datos con exito", EventLevel.Informational);
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Mesa - Error al actualizar mesa de la base de datos: {ex}", EventLevel.Error);
                //throw new Exception(ex.Message);
            }
        }
    }
}
