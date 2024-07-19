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

namespace DAL.Repositories.SqlServer
{
    class ClienteRepository : IGenericRepository<Cliente>
    {



        #region Statements
        private string InsertStatement
        {
            get => "INSERT INTO [dbo].[Cliente] (Id_Empresa,Id_Sucursal,Id_Cliente,Nombre,Apellido,Nro_Doc,Tipo_Doc,Estado_Civil,Fecha_Nacimiento,Sexo,Email,Nacionalidad,Fecha_Alta_Customer,Estado) VALUES (@Id_Empresa,@Id_Sucursal,@Id_Cliente,@Nombre,@Apellido,@Nro_Doc,@Tipo_Doc,@Estado_Civil,@Fecha_Nacimiento,@Sexo,@Email,@Nacionalidad,@Fecha_Alta_Customer,@Estado)";
        }

        private string UpdateStatementInactive
        {
            get => "UPDATE [dbo].[Cliente] SET Estado = @Estado WHERE  Id_Cliente = @Id_Cliente and Id_Empresa_ = @Id_Empresa";
        }

        private string UpdateStatement
        {
            get => "UPDATE [dbo].[Cliente] SET Nombre=@Nombre,Apellido=@Apellido,Nro_Doc=@Nro_Doc,Tipo_Doc=@Tipo_Doc,Estado_Civil=@Estado_Civil,Fecha_Nacimiento=@Fecha_Nacimiento,Sexo=@Sexo,Email=@Email,Nacionalidad=@Nacionalidad,Estado=@Estado WHERE  Id_Cliente = @Id_Cliente and Id_Empresa = @Id_Empresa";
        }

        private string DeleteStatement
        {
            get => "DELETE FROM [dbo].[Cliente] WHERE id_Cliente = @Id_Cliente and Id_Empresa = @Id_Empresa";
        }

        private string SelectOneStatement
        {
            get => "SELECT Id_Empresa,Id_Sucursal,Id_Cliente,Numero_Cliente,Nombre,Apellido,Nro_Doc,Tipo_Doc,Estado_Civil,Fecha_Nacimiento,Sexo,Email,Nacionalidad,Fecha_Alta_Customer,Estado FROM [dbo].[Cliente] WHERE Id_Cliente = @Id_Cliente amd Id_Empresa = @Id_Empresa";
        }

        private string SelectAllStatement
        {
            get => "SELECT Id_Empresa,Id_Sucursal,Id_Cliente,Numero_Cliente,Nombre,Apellido,Nro_Doc,Tipo_Doc,Estado_Civil,Fecha_Nacimiento,Sexo,Email,Nacionalidad,Fecha_Alta_Customer,Estado FROM [dbo].[Cliente] WHERE Id_Empresa = @Id_Empresa";
        }
        #endregion

        public void Delete(Cliente obj)
        {
            try
            {
                int y = SqlHelper.ExecuteNonQuery(UpdateStatementInactive, System.Data.CommandType.Text,
                                                   new SqlParameter[] {new SqlParameter("@Estado", false),
                                                   new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                                   new SqlParameter("@Id_Cliente", Guid.Parse(obj.Id_Cliente.ToString()))});
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Clientes - Error al borrar cliente de la base de datos: {ex}", EventLevel.Error);
            }
        }




        public IEnumerable<Cliente> GetAll(Cliente obj)
        {
            List<Cliente> clientes = new List<Cliente>();
            try
            {
                using (var dr = SqlHelper.ExecuteReader(SelectAllStatement, System.Data.CommandType.Text,
                                new SqlParameter[] { new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString()))}))
                {
                    while (dr.Read())
                    {
                        Cliente cliente = new Cliente();

                        object[] values = new object[dr.FieldCount];
                        dr.GetValues(values);

                        cliente = ClienteAdapter.Current.Adapt(values);
                        clientes.Add(cliente);
                    }


                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Clientes - Error al listar Clientes: {ex}", EventLevel.Error);
            }
            return clientes;
        }

        public Cliente GetOne(Cliente obj)
        {
            Cliente cliente = new Cliente();

            LoggerManager.Current.Write("DAL Clientes - Buscando Cliente en la Base de Datos", EventLevel.Informational);

            try
            {
                using (var dr = SqlHelper.ExecuteReader(SelectOneStatement, System.Data.CommandType.Text,
                                                        new SqlParameter[]
                                                        {
                                                         new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                                         new SqlParameter("@Id_Cliente", Guid.Parse(obj.Id_Cliente.ToString()))}))
                {
                    if (dr.Read())
                    {
                        //En este caso tendremos un solo registro...
                        object[] values = new object[dr.FieldCount];
                        dr.GetValues(values);

                        cliente = ClienteAdapter.Current.Adapt(values);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Clientes- Error al buscar clientes: {ex}", EventLevel.Error);
            }
            return cliente;
        }

        public void Insert(Cliente obj)
        {
            try
            {
                int x = SqlHelper.ExecuteNonQuery(InsertStatement,
                                               System.Data.CommandType.Text,
                                              new SqlParameter[] {
                                              
                                              /*new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Cliente.ToString())),
                                              new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Cliente.ToString())),
                                              new SqlParameter("@Id_Cliente", Guid.Parse(obj.Id_Cliente.ToString())),
                                              //new SqlParameter("@Numero_Cliente", obj.Numero_Cliente),
                                              new SqlParameter("@Nombre", obj.Nombre),
                                              new SqlParameter("@Apellido", obj.Apellido),
                                              new SqlParameter("@Nro_Doc", ValidarNull(obj.Nro_Doc)),
                                              new SqlParameter("@Tipo_Doc", ValidarNull(obj.Tipo_Doc)),
                                              new SqlParameter("@Estado_Civil", ValidarNull(obj.Estado_Civil)),
                                              new SqlParameter("@Fecha_Nacimiento", ValidarNull(obj.Fecha_Nacimiento)),
                                              new SqlParameter("@Sexo", ValidarNull(obj.Sexo)),
                                              new SqlParameter("Email", obj.Email),
                                              new SqlParameter("@Nacionalidad", ValidarNull(obj.Nacionalidad)),
                                              new SqlParameter("@Fecha_Alta_Customer", ValidarNull(obj.Fecha_Alta_Cliente)),
                                              new SqlParameter("@Estado", obj.Estado)*/

                                              new SqlParameter("@Id_Empresa", obj.Id_Empresa),
                                              new SqlParameter("@Id_Sucursal", obj.Id_Sucursal),
                                              new SqlParameter("@Id_Cliente", Guid.NewGuid()),
                                              //new SqlParameter("@Numero_Cliente", obj.Numero_Cliente),
                                              new SqlParameter("@Nombre", obj.Nombre),
                                              new SqlParameter("@Apellido", obj.Apellido),
                                              new SqlParameter("@Nro_Doc", ValidarNull(obj.Nro_Doc)),
                                              new SqlParameter("@Tipo_Doc", ValidarNull(obj.Tipo_Doc)),
                                              new SqlParameter("@Estado_Civil", ValidarNull(obj.Estado_Civil)),
                                              new SqlParameter("@Fecha_Nacimiento", ValidarNull(obj.Fecha_Nacimiento)),
                                              new SqlParameter("@Sexo", ValidarNull(obj.Sexo)),
                                              new SqlParameter("Email", obj.Email),
                                              new SqlParameter("@Nacionalidad", ValidarNull(obj.Nacionalidad)),
                                              new SqlParameter("@Fecha_Alta_Customer", DateTime.Now),
                                              new SqlParameter("@Estado", true)


                                               });
            }

            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Clientes - Error al ingresar cliente en base de datos: {ex}", EventLevel.Error);
            }
        }


        public void Update(Cliente obj)
        {
            try
            {
                int x = SqlHelper.ExecuteNonQuery(UpdateStatement,
                                                   System.Data.CommandType.Text,
                                                   new SqlParameter[] {
                                              new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Cliente.ToString())),
                                              //new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Cliente.ToString())),
                                              new SqlParameter("@Id_Cliente", Guid.Parse(obj.Id_Cliente.ToString())),
                                              //new SqlParameter("@Numero_Cliente", obj.Numero_Cliente),
                                              new SqlParameter("@Nombre", obj.Nombre),
                                              new SqlParameter("@Apellido", obj.Apellido),
                                              new SqlParameter("@Nro_Doc", ValidarNull(obj.Nro_Doc)),
                                              new SqlParameter("@Tipo_Doc", ValidarNull(obj.Tipo_Doc)),
                                              new SqlParameter("@Estado_Civil", ValidarNull(obj.Estado_Civil)),
                                              new SqlParameter("@Fecha_Nacimiento", ValidarNull(obj.Fecha_Nacimiento)),
                                              new SqlParameter("@Sexo", ValidarNull(obj.Sexo)),
                                              new SqlParameter("Email", obj.Email),
                                              new SqlParameter("@Nacionalidad", ValidarNull(obj.Nacionalidad)),
                                              //new SqlParameter("@Fecha_Alta_Customer", ValidarNull(obj.Fecha_Alta_Cliente)),
                                              new SqlParameter("@Estado", obj.Estado) });
            }

            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Clientes - Error al actualizar cliente en base de datos: {ex}", EventLevel.Error);
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



        //Convierto los nulls de BDD en NULLS C#
        //private object[] ValidarNullBDD(object[] obj)
        //{
        //    for (int i = 0; i <= obj.Length; i++)
        //    {
        //        if (obj[i] == DBNull.Value)
        //        {
        //            obj[i] = null;
        //        }
        //    }
        //    return obj;
        //}
    }
}
