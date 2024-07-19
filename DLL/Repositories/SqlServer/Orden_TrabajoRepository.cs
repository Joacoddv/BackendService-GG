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
    class Orden_TrabajoRepository : IGenericRepository<Orden_Trabajo>
    {
        #region Statements
        private string InsertStatement
        {
            get => "INSERT INTO [dbo].[ORDEN_TRABAJO] (Id_Empresa,Id_Sucursal ,Id_Orden ,Numero_Orden ,Id_Pedido,Id_Plato,Estado,Cantidad,Observaciones,Fecha_Create_Orden,Fecha_Modificacion_Orden) VALUES (@Id_Empresa,@Id_Sucursal ,@Id_Orden ,@Numero_Orden ,@Id_Pedido,@Id_Plato,@Estado,@Cantidad,@Observaciones,@Fecha_Create_Orden,@Fecha_Modificacion_Orden)";
        }


        private string UpdateStatement
        {
            get => "UPDATE [dbo].[ORDEN_TRABAJO] SET Numero_Orden = @Numero_Orden ,Id_Pedido =@Id_Pedido,Id_Plato=@Id_Plato,Estado=@Estado,Cantidad=@Cantidad,Observaciones=@Observaciones,Fecha_Create_Orden=@Fecha_Create_Orden,Fecha_Modificacion_Orden=@Fecha_Modificacion_Orden WHERE  Id_Orden = @Id_Orden and Id_Empresa = @Id_Empresa and Id_Sucursal = @Id_Sucursal";
        }

        private string DeleteStatement
        {
            get => "DELETE FROM [dbo].[ORDEN_TRABAJO] WHERE Id_Orden = @Id_Orden and Id_Empresa = @Id_Empresa and Id_Sucursal = @Id_Sucursal";
        }

        private string SelectOneStatement
        {
            get => "SELECT Id_Empresa,Id_Sucursal ,Id_Orden ,Numero_Orden ,Id_Pedido,Id_Plato,Estado,Cantidad,Observaciones,Fecha_Create_Orden,Fecha_Modificacion_Orden FROM [dbo].[ORDEN_TRABAJO] WHERE Id_Orden = @Id_Orden and Id_Empresa = @Id_Empresa and Id_Sucursal = @Id_Sucursal";
        }

        private string SelectAllStatement
        {
            get => "SELECT Id_Empresa,Id_Sucursal ,Id_Orden ,Numero_Orden ,Id_Pedido,Id_Plato,Estado,Cantidad,Observaciones,Fecha_Create_Orden,Fecha_Modificacion_Orden FROM [dbo].[ORDEN_TRABAJO]  WHERE Id_Empresa = @Id_Empresa and Id_Sucursal=@Id_Sucursal";
        }
        #endregion

        public void Delete(Orden_Trabajo obj)
        {
            try
            {
                LoggerManager.Current.Write("DAL Orden Trabajo - Eliminando Orden Trabajo en la Base de Datos", EventLevel.Informational);

                int y = SqlHelper.ExecuteNonQuery(DeleteStatement, System.Data.CommandType.Text,
                                                   new SqlParameter[] {
                                                   new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                                   new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                                   new SqlParameter("@Id_Orden", Guid.Parse(obj.Id_Orden_Trabajo.ToString()))});

                LoggerManager.Current.Write("DAL Orden Trabajo -  Orden Trabajo eliminada en la base de datos con exito", EventLevel.Informational);
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Orden Trabajo - Error al eliminar Orden Trabajo de la base de datos: {ex}", EventLevel.Error);
            }
        }

        public IEnumerable<Orden_Trabajo> GetAll(Orden_Trabajo obj)
        {
            List<Orden_Trabajo> ots = new List<Orden_Trabajo>();
            try
            {
                LoggerManager.Current.Write("DAL Orden Trabajo - Buscando todas las Orden Trabajo en la Base de Datos", EventLevel.Informational);
                using (var dr = SqlHelper.ExecuteReader(SelectAllStatement, System.Data.CommandType.Text,
                                new SqlParameter[] {
                                new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString()))}))
                {
                    while (dr.Read())
                    {
                        Orden_Trabajo ot = new Orden_Trabajo();

                        object[] values = new object[dr.FieldCount];
                        dr.GetValues(values);

                        ot = Orden_TrabajoAdapter.Current.Adapt(values);
                        ots.Add(ot);
                    }

                    LoggerManager.Current.Write("DAL Orden Trabajo -  Ordenes Tabajo buscadas en la base de datos con exito", EventLevel.Informational);
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Orden Trabajo - Error al buscar Orden Trabajo de la base de datos: {ex}", EventLevel.Error);
            }
            return ots;
        }

        public Orden_Trabajo GetOne(Orden_Trabajo obj)
        {
            Orden_Trabajo ot = new Orden_Trabajo();

            LoggerManager.Current.Write("DAL Orden Trabajo - Buscando una Orden Trabajo en la Base de Datos", EventLevel.Informational);

            try
            {
                using (var dr = SqlHelper.ExecuteReader(SelectOneStatement, System.Data.CommandType.Text,
                                new SqlParameter[] {
                                new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                new SqlParameter("@Id_Orden", Guid.Parse(obj.Id_Orden_Trabajo.ToString()))}))
                {
                    if (dr.Read())
                    {
                        //En este caso tendremos un solo registro...
                        object[] values = new object[dr.FieldCount];
                        dr.GetValues(values);

                        ot = Orden_TrabajoAdapter.Current.Adapt(values);
                    }
                }
                LoggerManager.Current.Write("DAL Orden Trabajo -  Orden Trabajo buscaada en la base de datos con exito", EventLevel.Informational);
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Orden Trabajo - Error al buscar una Orden Trabajo de la base de datos: {ex}", EventLevel.Error);
            }
            return ot;
        }

        public void Insert(Orden_Trabajo obj)
        {
            LoggerManager.Current.Write("DAL Orden Trabajo - Insertando Orden Trabajo en la Base de Datos", EventLevel.Informational);
            try
            {
                int x = SqlHelper.ExecuteNonQuery(InsertStatement,
                                                   System.Data.CommandType.Text,
                                                   new SqlParameter[] {
                                              new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                              new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                              new SqlParameter("@Id_Orden", Guid.Parse(obj.Id_Orden_Trabajo.ToString())),
                                              new SqlParameter("@Numero_Orden", obj.Numero_Orden),
                                              new SqlParameter("@Id_Pedido", obj.Pedido.Id_Pedido),
                                              new SqlParameter("@Id_Plato", obj.Plato.Id_Plato),
                                              new SqlParameter("@Estado", obj.EEstadoOT),
                                              new SqlParameter("@Cantidad", obj.Cantidad),
                                              new SqlParameter("@Observaciones", obj.Observaciones),
                                              new SqlParameter("@Fecha_Create_Orden", obj.Fecha_Creacion)});

                LoggerManager.Current.Write("DAL Orden Trabajo -  Orden Trabajo insertada en la base de datos con exito", EventLevel.Informational);
            }

            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Orden Trabajo - Error al insertar Orden Trabajo de la base de datos: {ex}", EventLevel.Error);
            }
        }


        public void Update(Orden_Trabajo obj)
        {
            LoggerManager.Current.Write("DAL Orden Trabajo - Actualizando Orden Trabajo en la Base de Datos", EventLevel.Informational);
            try
            {
                int x = SqlHelper.ExecuteNonQuery(UpdateStatement,
                                                   System.Data.CommandType.Text,
                                                   new SqlParameter[] {
                                              new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                              new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                              new SqlParameter("@Id_Orden", Guid.Parse(obj.Id_Orden_Trabajo.ToString())),
                                              new SqlParameter("@Numero_Orden", obj.Numero_Orden),
                                              new SqlParameter("@Id_Pedido", obj.Pedido.Id_Pedido),
                                              new SqlParameter("@Id_Plato", obj.Plato.Id_Plato),
                                              new SqlParameter("@Estado", obj.EEstadoOT),
                                              new SqlParameter("@Cantidad", obj.Cantidad),
                                              new SqlParameter("@Observaciones", obj.Observaciones),
                                              new SqlParameter("@Fecha_Create_Orden", obj.Fecha_Creacion),
                                              new SqlParameter("@Fecha_Modificacion_Orden", ValidarNull(obj.Fecha_Modificacion)),});

                LoggerManager.Current.Write("DAL Orden Trabajo -  Orden Trabajo actualizada en la base de datos con exito", EventLevel.Informational);
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Orden Trabajo - Error al actualizar Orden Trabajo de la base de datos: {ex}", EventLevel.Error);
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
