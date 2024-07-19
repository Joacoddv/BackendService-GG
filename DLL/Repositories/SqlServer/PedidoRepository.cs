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
    class PedidoRepository : IGenericRepository<Pedido>
    {


        #region Statements
        private string InsertStatement
        {
            get => "INSERT INTO [dbo].[PEDIDO] (Id_Empresa,Id_Sucursal,Id_Pedido,Tipo_Pedido,Id_Cliente,Id_Direccion,Id_Mesa,Fecha_Creacion, Fecha_Modificacion,Fecha_Entrega,Estado,Estado_Factura_Pedido,Monto) VALUES (@Id_Empresa,@Id_Sucursal,@Id_Pedido,@Tipo_Pedido,@Id_Cliente,@Id_Direccion,@Id_Mesa,@Fecha_Creacion,@Fecha_Modificacion,@Fecha_Entrega,@Estado,@Estado_Factura_Pedido,@Monto)";
        }

        private string UpdateStatement
        {
            get => "UPDATE [dbo].[PEDIDO] SET Tipo_Pedido = @Tipo_Pedido, Id_Cliente=@Id_Cliente,Id_Direccion=@Id_Direccion, Id_Mesa=@Id_Mesa, Fecha_Creacion=@Fecha_Creacion,Fecha_Entrega=@Fecha_Entrega,Fecha_Modificacion=@Fecha_Modificacion,Estado=@Estado,Estado_Factura_Pedido=@Estado_Factura_Pedido,Monto=@Monto WHERE  Id_Pedido= @Id_Pedido and Id_Empresa=@Id_Empresa and Id_Sucursal=@Id_Sucursal";
        }

        private string UpdateStatementEstado
        {
            get => "UPDATE [dbo].[PEDIDO] SET Estado=@Estado WHERE  Id_Pedido= @Id_Pedido and Id_Empresa=@Id_Empresa and Id_Sucursal=@Id_Sucursal";
        }

        private string DeleteStatement
        {
            get => "DELETE FROM [dbo].[PEDIDO] WHERE Id_Pedido= @Id_Pedido and Id_Empresa=@Id_Empresa and Id_Sucursal=@Id_Sucursal";
        }

        private string SelectOneStatement
        {
            get => "SELECT Id_Empresa,Id_Sucursal,Id_Pedido,Numero_Pedido,Tipo_Pedido,Id_Cliente,Id_Direccion,Id_Mesa,Fecha_Creacion,Fecha_Entrega,Fecha_Modificacion,Estado,Estado_Factura_Pedido,Monto FROM [dbo].[PEDIDO] WHERE  Id_Pedido= @Id_Pedido and Id_Empresa=@Id_Empresa and Id_Sucursal=@Id_Sucursal";
        }

        private string SelectAllStatement
        {
            get => "SELECT Id_Empresa,Id_Sucursal,Id_Pedido,Numero_Pedido,Tipo_Pedido,Id_Cliente,Id_Direccion,Id_Mesa,Fecha_Creacion,Fecha_Entrega,Fecha_Modificacion,Estado,Estado_Factura_Pedido,Monto FROM [dbo].[PEDIDO] WHERE Id_Empresa=@Id_Empresa and Id_Sucursal=@Id_Sucursal";
        }
        #endregion

        public void Delete(Pedido obj)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Pedido> GetAll(Pedido obj)
        {
            List<Pedido> pedidos = new List<Pedido>();
            try
            {
                LoggerManager.Current.Write("DAL Pedido - Buscando pedidos en la Base de Datos", EventLevel.Informational);
                using (var dr = SqlHelper.ExecuteReader(SelectAllStatement, System.Data.CommandType.Text,
                                new SqlParameter[] {
                                                        new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                                        new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString()))}))
                {
                    while (dr.Read())
                    {
                        Pedido pedido = new Pedido();

                        object[] values = new object[dr.FieldCount];
                        dr.GetValues(values);

                        pedido = PedidoAdapter.Current.Adapt(values);

                        pedidos.Add(pedido);
                    }


                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Pedido - Error al buscar pedidos de la base de datos: {ex}", EventLevel.Error);
            }
            return pedidos;
        }

        public Pedido GetOne(Pedido obj)
        {
            Pedido pedido = new Pedido();

            LoggerManager.Current.Write("DAL Pedido - Buscando un pedido en la Base de Datos", EventLevel.Informational);

            try
            {
                using (var dr = SqlHelper.ExecuteReader(SelectOneStatement, System.Data.CommandType.Text,
                                                        new SqlParameter[] { 
                                                        new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                                        new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                                        new SqlParameter("@Id_Pedido", Guid.Parse(obj.Id_Pedido.ToString())) }))
                {
                    if (dr.Read())
                    {
                        //En este caso tendremos un solo registro...
                        object[] values = new object[dr.FieldCount];
                        dr.GetValues(values);

                        pedido = PedidoAdapter.Current.Adapt(values);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Pedido - Error al buscar un pedido de la base de datos: {ex}", EventLevel.Error);
            }
            return pedido;
        }

        public void Insert(Pedido obj)
        {
            try
            {
                LoggerManager.Current.Write("DAL Pedido - Insertando un pedido en la Base de Datos", EventLevel.Informational);
                int x = SqlHelper.ExecuteNonQuery(InsertStatement, System.Data.CommandType.Text,
                                                                   new SqlParameter[] {
                                              new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                              new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                              new SqlParameter("@Id_Pedido", Guid.Parse(obj.Id_Pedido.ToString())),
                                              new SqlParameter("@Tipo_Pedido", obj.Tipo_Pedido),
                                              //new SqlParameter("@Numero_Pedido", obj.Numero_Pedido)
                                              new SqlParameter("@Id_Cliente",ValidarNull(Guid.Parse(obj.Cliente.Id_Cliente.ToString()))),
                                              new SqlParameter("@Id_Direccion",ValidarNull(Guid.Parse(obj.Direccion.Id_Direccion.ToString()))),
                                              new SqlParameter("@Id_Mesa", ValidarNull(Guid.Parse(obj.Mesa.Id_Mesa.ToString()))),
                                              new SqlParameter("@Fecha_Creacion", obj.Fecha_Creacion),
                                              new SqlParameter("@Fecha_Entrega", obj.Fecha_Entrega),
                                              new SqlParameter("@Fecha_Modificacion", obj.Fecha_Modificacion),
                                              new SqlParameter("@Estado", obj.Estado),
                                              new SqlParameter("@Estado_Factura_Pedido", obj.Estado_Factura_Pedido),
                                              new SqlParameter("@Monto", ValidarNull(obj.Monto))});
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Pedido - Error al insertar pedido de la base de datos: {ex}", EventLevel.Error);
            }
        }

        public void Update(Pedido obj)
        {
            try
            {
                LoggerManager.Current.Write("DAL Pedido - Actualizando un pedido en la Base de Datos", EventLevel.Informational);
                int x = SqlHelper.ExecuteNonQuery(UpdateStatement, System.Data.CommandType.Text,
                                                                       new SqlParameter[] {
                                              new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                              new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                              new SqlParameter("@Id_Pedido", Guid.Parse(obj.Id_Pedido.ToString())),
                                              //new SqlParameter("@Numero_Pedido", obj.Numero_Pedido),
                                              new SqlParameter("@Tipo_Pedido", obj.Tipo_Pedido),
                                              new SqlParameter("@Id_Cliente", ValidarNull(Guid.Parse(obj.Cliente.Id_Cliente.ToString()))),
                                              new SqlParameter("@Id_Direccion", ValidarNull(Guid.Parse(obj.Direccion.Id_Direccion.ToString()))),
                                              new SqlParameter("@Id_Mesa", ValidarNull(Guid.Parse(obj.Mesa.Id_Mesa.ToString()))),
                                              new SqlParameter("@Fecha_Creacion", obj.Fecha_Creacion),
                                              new SqlParameter("@Fecha_Entrega", obj.Fecha_Entrega),
                                              new SqlParameter("@Fecha_Modificacion", ValidarNull(obj.Fecha_Modificacion)),
                                              new SqlParameter("@Estado", obj.Estado),
                                              new SqlParameter("@Estado_Factura_Pedido", obj.Estado_Factura_Pedido),
                                              new SqlParameter("@Monto", obj.Monto)});

            }

            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Pedido - Error al actualizar pedido de la base de datos: {ex}", EventLevel.Error);
            }
        }

        private object ValidarNull(object obj)
        {
            if (obj == null || (obj is Guid && (Guid)obj == Guid.Empty)) //|| Convert.ToInt32(obj.ToString()) == 0)
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
