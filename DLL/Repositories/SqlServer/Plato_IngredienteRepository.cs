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
    class Plato_IngredienteRepository : IGenericRepository<Plato_Ingrediente>
    {


        #region Statements
        private string InsertStatement
        {
            get => "INSERT INTO [dbo].[PLATO_INGREDIENTE] (Id_Empresa,Id_Sucursal,Id_PI,Id_Plato,Id_Ingrediente,Cantidad_Ingrediente) VALUES (@Id_Empresa,@Id_Sucursal,@Id_PI,@Id_Plato,@Id_Ingrediente,@Cantidad_Ingrediente)";
        }

        private string UpdateStatement
        {
            get => "UPDATE [dbo].[PLATO_INGREDIENTE] SET Id_PI=@Id_PI,Id_Plato=@Id_Plato,Id_Ingrediente=@Id_Ingrediente,Cantidad_Ingrediente=@Cantidad_Ingrediente WHERE  Id_PI= @Id_PI and Id_Empresa=@Id_Empresa and Id_Sucursal=@Id_Sucursal";
        }

        private string UpdateStatementEstado
        {
            get => "UPDATE [dbo].[PLATO_INGREDIENTE] SET Estado=@Estado WHERE  Id_PI= @Id_PI and Id_Empresa=@Id_Empresa and Id_Sucursal=@Id_Sucursal";
        }

        private string DeleteStatement
        {
            get => "DELETE FROM [dbo].[PLATO_INGREDIENTE] WHERE Id_PI = @Id_PI and Id_Empresa=@Id_Empresa and Id_Sucursal=@Id_Sucursal";
        }

        private string SelectOneStatement
        {
            get => "SELECT Id_Empresa,Id_Sucursal,Id_PI,Numero_PI,Id_Plato,Id_Ingrediente,Cantidad_Ingrediente FROM [dbo].[PLATO_INGREDIENTE] WHERE  Id_PI= @Id_PI and Id_Empresa=@Id_Empresa and Id_Sucursal=@Id_Sucursal";
        }

        private string SelectAllStatement
        {
            get => "SELECT Id_Empresa,Id_Sucursal,Id_PI,Numero_PI,Id_Plato,Id_Ingrediente,Cantidad_Ingrediente FROM [dbo].[PLATO_INGREDIENTE] WHERE Id_Empresa=@Id_Empresa and Id_Sucursal=@Id_Sucursal";
        }
        #endregion

        public void Delete(Plato_Ingrediente obj)
        {
            try
            {
                LoggerManager.Current.Write("DAL Plato_Ingrediente - Eliminando Plato_Ingrediente en la Base de Datos", EventLevel.Informational);

                int y = SqlHelper.ExecuteNonQuery(DeleteStatement, System.Data.CommandType.Text,
                                                   new SqlParameter[] {
                                                   new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                                   new SqlParameter("@Id_Sucursal",  Guid.Parse(obj.Id_Sucursal.ToString())),
                                                   new SqlParameter("@Id_PI", Guid.Parse(obj.Id_PI.ToString()))
                                                   });
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Plato_Ingrediente - Error al eliminar Plato_Ingrediente de la base de datos: {ex}", EventLevel.Error);
            }
        }

        public IEnumerable<Plato_Ingrediente> GetAll(Plato_Ingrediente obj)
        {
            List<Plato_Ingrediente> plato_Ingredientes = new List<Plato_Ingrediente>();
            try
            {
                LoggerManager.Current.Write("DAL Plato_Ingrediente - Buscando Plato_Ingredientes en la Base de Datos", EventLevel.Informational);

                using (var dr = SqlHelper.ExecuteReader(SelectAllStatement, System.Data.CommandType.Text,
                                new SqlParameter[] {
                                new SqlParameter("@Id_Empresa",  Guid.Parse(obj.Id_Empresa.ToString())),
                                new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString()))}))
                {
                    while (dr.Read())
                    {
                        Plato_Ingrediente plato_ingrediente = new Plato_Ingrediente();

                        object[] values = new object[dr.FieldCount];
                        dr.GetValues(values);

                        plato_ingrediente = Plato_IngredienteAdapter.Current.Adapt(values);

                        plato_Ingredientes.Add(plato_ingrediente);
                    }


                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Plato_Ingrediente - Error al buscar Plato_Ingredientes de la base de datos: {ex}", EventLevel.Error);
            }
            return plato_Ingredientes;
        }

        public Plato_Ingrediente GetOne(Plato_Ingrediente obj)
        {
            Plato_Ingrediente plato_ingrediente = new Plato_Ingrediente();

            LoggerManager.Current.Write("DAL Plato_Ingrediente - Buscando un Plato_Ingrediente en la Base de Datos", EventLevel.Informational);

            try
            {
                using (var dr = SqlHelper.ExecuteReader(SelectOneStatement, System.Data.CommandType.Text,
                                                        new SqlParameter[] {
                                                        new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                                        new SqlParameter("@Id_Sucursal",  Guid.Parse(obj.Id_Sucursal.ToString())),
                                                        new SqlParameter("@Id_PI", Guid.Parse(obj.Id_PI.ToString()))}))
                {
                    if (dr.Read())
                    {
                        //En este caso tendremos un solo registro...
                        object[] values = new object[dr.FieldCount];
                        dr.GetValues(values);

                        plato_ingrediente = Plato_IngredienteAdapter.Current.Adapt(values);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Plato_Ingrediente - Error al buscar un Plato_Ingrediente de la base de datos: {ex}", EventLevel.Error);
            }
            return plato_ingrediente;
        }

        public void Insert(Plato_Ingrediente obj)
        {
            try
            {
                LoggerManager.Current.Write("DAL Plato_Ingrediente - Insertando Plato_Ingrediente en la Base de Datos", EventLevel.Informational);
                int x = SqlHelper.ExecuteNonQuery(InsertStatement, System.Data.CommandType.Text,
                                                                       new SqlParameter[] {
                                              new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                              new SqlParameter("@Id_Sucursal",  Guid.Parse(obj.Id_Sucursal.ToString())),
                                              new SqlParameter("@Id_PI", Guid.Parse(obj.Id_PI.ToString())),
                                              //new SqlParameter("@Numero_PI", obj.Numero_PI),
                                              new SqlParameter("@Id_Plato",  Guid.Parse(obj.Plato.Id_Plato.ToString())),
                                              new SqlParameter("@Id_Ingrediente", Guid.Parse(obj.Ingrediente.Id_Ingrediente.ToString())),
                                              new SqlParameter("@Cantidad_Ingrediente", obj.Cantidad_Ingrediente)});
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Plato_Ingrediente - Error al insertar Plato_Ingrediente de la base de datos: {ex}", EventLevel.Error);
            }
        }

        public void Update(Plato_Ingrediente obj)
        {
            try
            {
                LoggerManager.Current.Write("DAL Plato_Ingrediente - Actualizando Plato_Ingrediente en la Base de Datos", EventLevel.Informational);

                int x = SqlHelper.ExecuteNonQuery(UpdateStatement, System.Data.CommandType.Text,
                                                                       new SqlParameter[] {
                                              new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                              new SqlParameter("@Id_Sucursal",  Guid.Parse(obj.Id_Sucursal.ToString())),
                                              new SqlParameter("@Id_PI", Guid.Parse(obj.Id_PI.ToString())),
                                              //new SqlParameter("@Numero_PI", obj.Numero_PI),
                                              new SqlParameter("@Id_Plato",  Guid.Parse(obj.Plato.Id_Plato.ToString())),
                                              new SqlParameter("@Id_Ingrediente", Guid.Parse(obj.Ingrediente.Id_Ingrediente.ToString())),
                                              new SqlParameter("@Cantidad_Ingrediente", obj.Cantidad_Ingrediente)});

            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Plato_Ingrediente - Error al actualizar Plato_Ingrediente de la base de datos: {ex}", EventLevel.Error);
            }
        }
    }
}
