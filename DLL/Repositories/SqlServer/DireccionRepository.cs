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
    class DireccionRepository : IGenericRepository<Direccion>
    {
        #region Statements
        private string InsertStatement
        {
            get => "INSERT INTO [dbo].[Direcciones] (Id_Empresa,Id_Sucursal,Id_Direccion,Id_Cliente,Numero_Cliente,TIPO_DIRECCION ,Telefono_CEL,Telefono_CASA,Telefono_OTRO ,Direccion ,Altura ,Piso ,Localidad) VALUES (@Id_Empresa,@Id_Sucursal,@Id_Direccion ,@Id_Cliente,@Numero_Cliente,@TIPO_DIRECCION ,@Telefono_CEL,@Telefono_CASA,@Telefono_OTRO ,@Direccion ,@Altura ,@Piso ,@Localidad)";
        }

        private string UpdateStatementInactive
        {
            get => "UPDATE [dbo].[Direcciones] SET Estado = @Estado WHERE  Id_Direccion = @Id_Direccion and Id_Empresa = @Id_Empresa";
        }

        private string UpdateStatement
        {
            get => "UPDATE [dbo].[Direcciones] SET Id_Direccion = @Id_Direccion,Id_Cliente =@Id_Cliente ,Numero_Cliente=@Numero_Cliente,TIPO_DIRECCION=@TIPO_DIRECCION,Telefono_CEL=@Telefono_CEL,Telefono_CASA=@Telefono_CASA,Telefono_OTRO=@Telefono_OTRO,Direccion=@Direccion,Altura=@Altura,Piso=@Piso,Localidad=@Localidad WHERE  Id_Direccion = @Id_Direccion and Id_Empresa= @Id_Empresa";
        }

        private string DeleteStatement
        {
            get => "DELETE FROM [dbo].[Direcciones] WHERE Id_Direccion = @Id_Direccion and Id_Empresa = @Id_Empresa";
        }

        private string SelectOneStatement
        {
            get => "SELECT Id_Empresa,Id_Sucursal,Id_Direccion ,Id_Cliente,Numero_Direccion,Numero_Cliente,TIPO_DIRECCION,Telefono_CEL,Telefono_CASA,Telefono_OTRO ,Direccion ,Altura ,Piso ,Localidad FROM [dbo].[Direcciones] WHERE Id_Direccion = @Id_Direccion and Id_Empresa=@Id_Empresa";
        }

        private string SelectAllStatement
        {
            get => "SELECT Id_Empresa,Id_Sucursal,Id_Direccion ,Id_Cliente,Numero_Direccion,Numero_Cliente,TIPO_DIRECCION ,Telefono_CEL,Telefono_CASA,Telefono_OTRO ,Direccion ,Altura ,Piso ,Localidad FROM [dbo].[Direcciones] WHERE Id_Empresa=@Id_Empresa";
        }
        #endregion

        public void Delete(Direccion obj)
        {
            try
            {
                LoggerManager.Current.Write("DAL Direcciones - Borrando Direccion en la Base de Datos", EventLevel.Informational);

                int y = SqlHelper.ExecuteNonQuery(DeleteStatement, System.Data.CommandType.Text,
                                                   new SqlParameter[]
                                                   {
                                                   new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                                   new SqlParameter("@Id_Direccion", Guid.Parse(obj.Id_Direccion.ToString()))});
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Direcciones - Error al borrar dirección de la base de datos: {ex}", EventLevel.Error);
            }
        }

        public IEnumerable<Direccion> GetAll(Direccion obj)
        {
            List<Direccion> direcciones = new List<Direccion>();
            try
            {
                LoggerManager.Current.Write("DAL Direcciones - Buscando todas las direcciones en la Base de Datos", EventLevel.Informational);

                using (var dr = SqlHelper.ExecuteReader(SelectAllStatement, System.Data.CommandType.Text,
                                new SqlParameter[] { new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())) }))
                {
                    while (dr.Read())
                    {
                        Direccion direccion = new Direccion();

                        object[] values = new object[dr.FieldCount];
                        dr.GetValues(values);

                        direccion = DireccionesAdapter.Current.Adapt(values);
                        direcciones.Add(direccion);
                    }


                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Direcciones - Error al buscar todas las direcciones de la base de datos: {ex}", EventLevel.Error);
            }
            return direcciones;
        }

        public Direccion GetOne(Direccion obj)
        {
            Direccion Direcciones = new Direccion();

            LoggerManager.Current.Write("DAL Direcciones - Buscando Direccion en la Base de Datos", EventLevel.Informational);

            try
            {
                using (var dr = SqlHelper.ExecuteReader(SelectOneStatement, System.Data.CommandType.Text,
                                                        new SqlParameter[] {
                                                        new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                                        new SqlParameter("@Id_Direccion", Guid.Parse(obj.Id_Direccion.ToString()))}))
                {
                    if (dr.Read())
                    {
                        //En este caso tendremos un solo registro...
                        object[] values = new object[dr.FieldCount];
                        dr.GetValues(values);

                        Direcciones = DireccionesAdapter.Current.Adapt(values);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Direcciones - Error al buscar una dirección de la base de datos: {ex}", EventLevel.Error);
            }
            return Direcciones;
        }

        public void Insert(Direccion obj)
        {
            try
            {

                LoggerManager.Current.Write("DAL Direcciones - Insertando dirección en la Base de Datos", EventLevel.Informational);

                int x = SqlHelper.ExecuteNonQuery(InsertStatement,
                                               System.Data.CommandType.Text,
                                               new SqlParameter[] {
                                              new SqlParameter("@Id_Direccion", Guid.Parse(obj.Id_Direccion.ToString())),
                                              new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                              new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                              new SqlParameter("@Id_Cliente", Guid.Parse(obj.Cliente.Id_Cliente.ToString())),
                                              //new SqlParameter("@Numero_Direccion", obj.Numero_Direccion),
                                              new SqlParameter("@Numero_Cliente", obj.Cliente.Numero_Cliente),
                                              new SqlParameter("@TIPO_DIRECCION", ValidarNull(obj.Tipo_Direccion)),
                                              new SqlParameter("@Telefono_CEL", ValidarNull(obj.Telefono_Cel)),
                                              new SqlParameter("@Telefono_CASA", ValidarNull(obj.Telefono_Casa)),
                                              new SqlParameter("@Telefono_OTRO", ValidarNull(obj.Telefono_Otro)),
                                              new SqlParameter("@Direccion",ValidarNull( obj.Nombre_Calle)),
                                              new SqlParameter("@Altura", ValidarNull(obj.Altura)),
                                              new SqlParameter("@Piso", ValidarNull(obj.Piso)),
                                              new SqlParameter("@Localidad",ValidarNull( obj.Localidad))});

            }

            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Direcciones - Error al buscar una dirección de la base de datos: {ex}", EventLevel.Error);
            }
        }



        public void Update(Direccion obj)
        {
            try
            {
                LoggerManager.Current.Write("DAL Direcciones - Actualizando dirección en la Base de Datos", EventLevel.Informational);

                int x = SqlHelper.ExecuteNonQuery(UpdateStatement,
                                                   System.Data.CommandType.Text,
                                                   new SqlParameter[] {
                                              new SqlParameter("@Id_Direccion", Guid.Parse(obj.Id_Direccion.ToString())),
                                              new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                              //new SqlParameter("@Numero_Direccion", obj.Numero_Direccion),
                                              new SqlParameter("@Numero_Direccion", obj.Numero_Direccion),
                                              new SqlParameter("@Id_Cliente", Guid.Parse(obj.Cliente.Id_Cliente.ToString())),
                                              new SqlParameter("@Numero_Cliente", obj.Cliente.Numero_Cliente),
                                              new SqlParameter("@TIPO_DIRECCION", ValidarNull(obj.Tipo_Direccion)),
                                              new SqlParameter("@Telefono_CEL", ValidarNull(obj.Telefono_Cel)),
                                              new SqlParameter("@Telefono_CASA", ValidarNull(obj.Telefono_Casa)),
                                              new SqlParameter("@Telefono_OTRO", ValidarNull(obj.Telefono_Otro)),
                                              new SqlParameter("@Direccion",ValidarNull( obj.Nombre_Calle)),
                                              new SqlParameter("@Altura", ValidarNull(obj.Altura)),
                                              new SqlParameter("@Piso", ValidarNull(obj.Piso)),
                                              new SqlParameter("@Localidad",ValidarNull( obj.Localidad))});
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Direcciones - Error al actualizar dirección de la base de datos: {ex}", EventLevel.Error);
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
