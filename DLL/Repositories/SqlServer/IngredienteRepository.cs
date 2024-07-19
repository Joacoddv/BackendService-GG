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
    class IngredienteRepository : IGenericRepository<Ingrediente>
    {


        #region Statements
        private string InsertStatement
        {
            get => "INSERT INTO [dbo].[INGREDIENTES] (Id_Empresa,Id_Sucursal,Id_Ingredientes,Nombre_Ingrediente,Descripcion,Medida,Estado) VALUES (@Id_Empresa,@Id_Sucursal,@Id_Ingredientes,@Nombre_Ingrediente,@Descripcion,@Medida,@Estado)";
        }

        private string UpdateStatement
        {
            get => "UPDATE [dbo].[INGREDIENTES] SET Id_Ingredientes=@Id_Ingredientes,Nombre_Ingrediente=@Nombre_Ingrediente,Descripcion=@Descripcion,Medida=@Medida,Estado=@Estado WHERE  Id_Ingredientes= @Id_Ingredientes and Id_Empresa = @Id_Empresa and Id_Sucursal=@Id_Sucursal";
        }

        private string UpdateStatementEstado
        {
            get => "UPDATE [dbo].[INGREDIENTES] SET Estado=@Estado WHERE  Id_Ingredientes= @Id_Ingredientes and Id_Empresa = @Id_Empresa and Id_Sucursal=@Id_Sucursal";
        }

        private string DeleteStatement
        {
            get => "DELETE FROM [dbo].[INGREDIENTES] WHERE Id_Ingredientes = @Id_Ingredientes and Id_Empresa = @Id_Empresa and Id_Sucursal=@Id_Sucursal";
        }

        private string SelectOneStatement
        {
            get => "SELECT Id_Empresa,Id_Sucursal,Id_Ingredientes,Numero_Ingrediente,Nombre_Ingrediente,Descripcion,Medida,Estado FROM [dbo].[INGREDIENTES] WHERE  Id_Ingredientes= @Id_Ingredientes and Id_Empresa = @Id_Empresa and Id_Sucursal=@Id_Sucursal";
        }

        private string SelectAllStatement
        {
            get => "SELECT Id_Empresa,Id_Sucursal,Id_Ingredientes,Numero_Ingrediente,Nombre_Ingrediente,Descripcion,Medida,Estado FROM [dbo].[INGREDIENTES] where Id_Empresa = @Id_Empresa";
        }
        #endregion

        public void Delete(Ingrediente obj)
        {
            try
            {
                LoggerManager.Current.Write("DAL Ingrediente - Eliminando Ingrediente en la Base de Datos", EventLevel.Informational);

                int y = SqlHelper.ExecuteNonQuery(UpdateStatementEstado, System.Data.CommandType.Text,
                                                   new SqlParameter[] {
                                                   new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                                   new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                                   new SqlParameter("@Id_Ingredientes", Guid.Parse(obj.Id_Ingrediente.ToString())),
                                                       new SqlParameter("@Estado", false)});
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Ingrediente - Error al eliminar ingrediente de la base de datos: {ex}", EventLevel.Error);
            }
        }

        public IEnumerable<Ingrediente> GetAll(Ingrediente obj)
        {
            List<Ingrediente> ingredientes = new List<Ingrediente>();
            try
            {
                LoggerManager.Current.Write("DAL Ingrediente - Buscando Ingredientes en la Base de Datos", EventLevel.Informational);
                using (var dr = SqlHelper.ExecuteReader(SelectAllStatement, System.Data.CommandType.Text,
                                new SqlParameter[] {
                                new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString()))}))
                {
                    while (dr.Read())
                    {
                        Ingrediente ingrediente = new Ingrediente();

                        object[] values = new object[dr.FieldCount];
                        dr.GetValues(values);

                        ingrediente = IngredienteAdapter.Current.Adapt(values);

                        ingredientes.Add(ingrediente);
                    }


                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Ingrediente - Error al buscar ingredientes de la base de datos: {ex}", EventLevel.Error);
            }
            return ingredientes;
        }

        public Ingrediente GetOne(Ingrediente obj)
        {
            Ingrediente ingrediente = new Ingrediente();

            LoggerManager.Current.Write("DAL Ingrediente - Buscando un Ingrediente en la Base de Datos", EventLevel.Informational);

            try
            {
                using (var dr = SqlHelper.ExecuteReader(SelectOneStatement, System.Data.CommandType.Text,
                                                        new SqlParameter[] {
                                                   new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                                   new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                                   new SqlParameter("@Id_Ingredientes", Guid.Parse(obj.Id_Ingrediente.ToString())), }))
                {
                    if (dr.Read())
                    {
                        //En este caso tendremos un solo registro...
                        object[] values = new object[dr.FieldCount];
                        dr.GetValues(values);

                        ingrediente = IngredienteAdapter.Current.Adapt(values);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Ingrediente - Error al buscar un ingrediente de la base de datos: {ex}", EventLevel.Error);
            }
            return ingrediente;
        }

        public void Insert(Ingrediente obj)
        {
            try
            {
                LoggerManager.Current.Write("DAL Ingrediente - Ingresando Ingrediente en la Base de Datos", EventLevel.Informational);
                int x = SqlHelper.ExecuteNonQuery(InsertStatement, System.Data.CommandType.Text,
                                                                       new SqlParameter[] {
                                              new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                              new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                              new SqlParameter("@Id_Ingredientes", Guid.Parse(obj.Id_Ingrediente.ToString())),
                                              //new SqlParameter("@Numero_Ingrediente", obj.Numero_Ingrediente),
                                              new SqlParameter("@Nombre_Ingrediente", obj.Nombre_Ingrediente),
                                              new SqlParameter("@Descripcion", obj.Descripcion),
                                              new SqlParameter("@Medida", obj.Medida),
                                              new SqlParameter("@Estado", obj.Estado)});
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Ingrediente - Error al ingresar ingrediente de la base de datos: {ex}", EventLevel.Error);
            }
        }

        public void Update(Ingrediente obj)
        {
            try
            {
                LoggerManager.Current.Write("DAL Ingrediente - Actualizando Ingrediente en la Base de Datos", EventLevel.Informational);
                int x = SqlHelper.ExecuteNonQuery(UpdateStatement, System.Data.CommandType.Text,
                                                                       new SqlParameter[] {
                                              new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                              new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                              new SqlParameter("@Id_Ingredientes", Guid.Parse(obj.Id_Ingrediente.ToString())),
                                              //new SqlParameter("@Numero_Ingrediente", obj.Numero_Ingrediente),
                                              new SqlParameter("@Nombre_Ingrediente", obj.Nombre_Ingrediente),
                                              new SqlParameter("@Descripcion", obj.Descripcion),
                                              new SqlParameter("@Medida", obj.Medida),
                                              new SqlParameter("@Estado", obj.Estado)});

            }

            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Ingriedente - Error al actualizar ingrediente de la base de datos: {ex}", EventLevel.Error);
            }
        }
    }
}
