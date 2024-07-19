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
    class PlatoRepository : IGenericRepository<Plato>
    {


        #region Statements
        private string InsertStatement
        {
            get => "INSERT INTO [dbo].[PLATO] (Id_Empresa,Id_Sucursal,Id_Plato,Nombre_Plato,Descripcion,Estado) VALUES (@Id_Empresa,@Id_Sucursal,@Id_Plato,@Nombre_Plato,@Descripcion,@Estado)";
        }

        private string UpdateStatement
        {
            get => "UPDATE [dbo].[PLATO] SET Id_Plato=@Id_Plato,Nombre_Plato=@Nombre_Plato,Descripcion=@Descripcion,Estado=@Estado WHERE  Id_Plato= @Id_Plato and Id_Empresa = @Id_Empresa and Id_Sucursal=@Id_Sucursal";
        }

        private string UpdateStatementEstado
        {
            get => "UPDATE [dbo].[PLATO] SET Estado=@Estado WHERE  Id_Plato= @Id_Plato and Id_Empresa = @Id_Empresa and Id_Sucursal = @Id_Sucursal";
        }

        private string DeleteStatement
        {
            get => "DELETE FROM [dbo].[PLATO] WHERE Id_Plato = @Id_Plato and Id_Empresa = @Id_Empresa and Id_Sucursal = @Id_Sucursal";
        }

        private string SelectOneStatement
        {
            get => "SELECT Id_Empresa,Id_Sucursal,Id_Plato,Numero_Plato,Nombre_Plato,Descripcion,Estado FROM [dbo].[PLATO] WHERE  Id_Plato= @Id_Plato and Id_Empresa = @Id_Empresa and Id_Sucursal = @Id_Sucursal";
        }

        private string SelectAllStatement
        {
            get => "SELECT Id_Empresa,Id_Sucursal,Id_Plato,Numero_Plato,Nombre_Plato,Descripcion,Estado FROM [dbo].[PLATO] WHERE Id_Empresa = @Id_Empresa and Id_Sucursal = @Id_Sucursal";
        }
        #endregion

        public void Delete(Plato obj)
        {
            try
            {
                LoggerManager.Current.Write("DAL Plato - Eliminando plato en la base de datos", EventLevel.Informational);

                int y = SqlHelper.ExecuteNonQuery(UpdateStatementEstado, System.Data.CommandType.Text,
                                                   new SqlParameter[] {
                                                   new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                                   new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                                   new SqlParameter("@Id_Plato", Guid.Parse(obj.Id_Plato.ToString())),
                                                   new SqlParameter("@Estado", false)});
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Plato - Error al borrar Platos de la base de datos: {ex}", EventLevel.Error);
            }
        }

        public IEnumerable<Plato> GetAll(Plato obj)
        {
            List<Plato> platos = new List<Plato>();
            try
            {
                LoggerManager.Current.Write("DAL Plato - Buscando platos en la base de datos", EventLevel.Informational);

                using (var dr = SqlHelper.ExecuteReader(SelectAllStatement, System.Data.CommandType.Text,
                                new SqlParameter[] {
                                new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString()))}))
                {
                    while (dr.Read())
                    {
                        Plato plato = new Plato();

                        object[] values = new object[dr.FieldCount];
                        dr.GetValues(values);

                        plato = PlatoAdapter.Current.Adapt(values);

                        platos.Add(plato);
                    }


                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Plato - Error al buscar Platos de la base de datos: {ex}", EventLevel.Error);
            }
            return platos;
        }

        public Plato GetOne(Plato obj)
        {
            Plato plato = new Plato();

            LoggerManager.Current.Write("DAL Plato - Buscando un plato en la base de datos", EventLevel.Informational);

            try
            {
                using (var dr = SqlHelper.ExecuteReader(SelectOneStatement, System.Data.CommandType.Text,
                                                        new SqlParameter[] {
                                                        new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                                        new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                                        new SqlParameter("@Id_Plato", Guid.Parse(obj.Id_Plato.ToString())),
                                                        }))
                {
                    if (dr.Read())
                    {
                        //En este caso tendremos un solo registro...
                        object[] values = new object[dr.FieldCount];
                        dr.GetValues(values);

                        plato = PlatoAdapter.Current.Adapt(values);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Plato - Error al buscar un Plato de la base de datos: {ex}", EventLevel.Error);
            }
            return plato;
        }

        public void Insert(Plato obj)
        {
            try
            {
                LoggerManager.Current.Write("DAL Plato - Insertando plato en la base de datos", EventLevel.Informational);

                int x = SqlHelper.ExecuteNonQuery(InsertStatement, System.Data.CommandType.Text,
                                                                       new SqlParameter[] {
                                              new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                              new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                              new SqlParameter ("@Id_Plato", Guid.Parse(obj.Id_Plato.ToString())),
                                              //new SqlParameter("@Numero_Plato", obj.Numero_Plato),
                                              new SqlParameter("@Nombre_Plato", obj.Nombre_Plato),
                                              new SqlParameter("@Descripcion", obj.Descripcion),
                                              new SqlParameter("@Estado", obj.Estado)});
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Plato - Error al insertando Plato de la base de datos: {ex}", EventLevel.Error);
            }
        }

        public void Update(Plato obj)
        {
            try
            {
                LoggerManager.Current.Write("DAL Plato - Actualizando plato en la base de datos", EventLevel.Informational);

                int x = SqlHelper.ExecuteNonQuery(UpdateStatement, System.Data.CommandType.Text,
                                                                       new SqlParameter[] {
                                              new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                              new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                              new SqlParameter ("@Id_Plato", Guid.Parse(obj.Id_Plato.ToString())),
                                              //new SqlParameter("@Numero_Plato", obj.Numero_Plato),
                                              new SqlParameter("@Nombre_Plato", obj.Nombre_Plato),
                                              new SqlParameter("@Descripcion", obj.Descripcion),
                                              new SqlParameter("@Estado", obj.Estado)});

            }

            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Plato - Error al actualizar Plato de la base de datos: {ex}", EventLevel.Error);
            }
        }
    }
}
