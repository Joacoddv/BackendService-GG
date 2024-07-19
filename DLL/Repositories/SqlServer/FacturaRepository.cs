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
    class FacturaRepository : IGenericRepository<Factura>
    {


        #region Statements
        private string InsertStatement
        {
            get => "INSERT INTO [dbo].[FACTURA] (Id_Empresa,Id_Sucursal,Id_Factura,Fecha_Alta_Factura,Id_Cliente,Estado,Sub_Total,Total_Iva,Total_Factura) VALUES (@Id_Empresa,@Id_Sucursal,@Id_Factura,@Fecha_Alta_Factura,@Id_Cliente,@Estado,@Sub_Total,@Total_Iva,@Total_Factura)";
        }

        private string UpdateStatement
        {
            get => "UPDATE [dbo].[Factura] SET Fecha_Alta_Factura=@Fecha_Alta_Factura,Estado=@Estado,Sub_Total=@Sub_Total,Total_Iva=@Total_Iva,Total_Factura=@Total_Factura  WHERE  Id_Factura= @Id_Factura and Id_Empresa=@Id_Empresa and Id_Sucursal = @Id_Sucursal";
        }

        private string UpdateStatementEstado
        {
            get => "UPDATE [dbo].[Factura] SET Estado=@Estado WHERE  Id_Factura= @Id_Factura and Id_Empresa=@Id_Empresa and Id_Sucursal = @Id_Sucursal";
        }

        private string DeleteStatement
        {
            get => "DELETE FROM [dbo].[Factura] WHERE Id_Factura = @Id_Factura and Id_Empresa=@Id_Empresa and Id_Sucursal = @Id_Sucursal";
        }

        private string SelectOneStatement
        {
            get => "SELECT Id_Empresa,Id_Sucursal,Id_Factura,Numero_Factura,Fecha_Alta_Factura,Id_Cliente,Estado,Sub_Total,Total_Iva,Total_Factura FROM [dbo].[Factura] WHERE  Id_Factura= @Id_Factura and Id_Empresa=@Id_Empresa and Id_Sucursal = @Id_Sucursal";
        }

        private string SelectAllStatement
        {
            get => "SELECT Id_Empresa,Id_Sucursal,Id_Factura,Numero_Factura,Fecha_Alta_Factura,Id_Cliente,Estado,Sub_Total,Total_Iva,Total_Factura FROM [dbo].[Factura] WHERE Id_Empresa=@Id_Empresa and Id_Sucursal = @Id_Sucursal";
        }
        #endregion

        public void Delete(Factura obj)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<Factura> GetAll(Factura obj)
        {
            List<Factura> facturas = new List<Factura>();
            try
            {
                LoggerManager.Current.Write("DAL Factura - Buscando Facturas en la Base de Datos", EventLevel.Informational);
                using (var dr = SqlHelper.ExecuteReader(SelectAllStatement, System.Data.CommandType.Text,
                                new SqlParameter[] {
                                new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString()))}))
                {
                    while (dr.Read())
                    {
                        Factura factura = new Factura();

                        object[] values = new object[dr.FieldCount];
                        dr.GetValues(values);

                        factura = FacturaAdapter.Current.Adapt(values);

                        facturas.Add(factura);
                    }


                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Factura - Error al buscar Facturas de la base de datos: {ex}", EventLevel.Error);
            }
            return facturas;
        }

        public Factura GetOne(Factura obj)
        {
            Factura factura = new Factura();

            LoggerManager.Current.Write("DAL Factura - Buscando una Factura en la Base de Datos", EventLevel.Informational);

            try
            {
                using (var dr = SqlHelper.ExecuteReader(SelectOneStatement, System.Data.CommandType.Text,
                                                        new SqlParameter[] {
                                                        new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                                        new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                                        new SqlParameter("@Id_Factura", Guid.Parse(obj.Id_Factura.ToString())) }))
                {
                    if (dr.Read())
                    {
                        //En este caso tendremos un solo registro...
                        object[] values = new object[dr.FieldCount];
                        dr.GetValues(values);

                        factura = FacturaAdapter.Current.Adapt(values);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Factura - Error al buscar una Factura de la base de datos: {ex}", EventLevel.Error);
            }
            return factura;
        }

        public void Insert(Factura obj)
        {
            try
            {
                //LoggerManager.Current.Write("DAL Factura - Insertando Factura en la Base de Datos", EventLevel.Informational);
                int x = SqlHelper.ExecuteNonQuery(InsertStatement, System.Data.CommandType.Text,
                                                                       new SqlParameter[] {
                                              new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                              new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                              new SqlParameter("@Id_Factura", Guid.Parse(obj.Id_Factura.ToString())),
                                              //new SqlParameter("@Numero_Factura", obj.Numero_Factura),
                                              new SqlParameter("@Fecha_Alta_Factura", DateTime.Now),
                                              new SqlParameter("@Id_Cliente", Guid.Parse(obj.Cliente.Id_Cliente.ToString())),
                                              new SqlParameter("@Estado", obj.Estado),
                                              new SqlParameter("@Sub_Total", obj.Sub_Total),
                                              new SqlParameter("@Total_Iva", obj.Total_Iva),
                                              new SqlParameter("@Total_Factura", obj.Total_Factura)});
            }
            catch (Exception ex)
            {
               // LoggerManager.Current.Write($"DAL Factura - Error al insertar Factura en la base de datos: {ex}", EventLevel.Error);
            }
        }

        public void Update(Factura obj)
        {
            try
            {
                LoggerManager.Current.Write("DAL Factura - Actualizando Factura en la Base de Datos", EventLevel.Informational);
                int x = SqlHelper.ExecuteNonQuery(UpdateStatement, System.Data.CommandType.Text,
                                                                       new SqlParameter[] {
                                              new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                              new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                              new SqlParameter("@Id_Factura", Guid.Parse(obj.Id_Factura.ToString())),
                                              //new SqlParameter("@Numero_Factura", obj.Numero_Factura),
                                              new SqlParameter("@Fecha_Alta_Factura", DateTime.Now),
                                              new SqlParameter("@Id_Cliente", Guid.Parse(obj.Cliente.Id_Cliente.ToString())),
                                              new SqlParameter("@Estado", obj.Estado),
                                              new SqlParameter("@Sub_Total", obj.Sub_Total),
                                              new SqlParameter("@Total_Iva", obj.Total_Iva),
                                              new SqlParameter("@Total_Factura", obj.Total_Factura)});

            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Factura - Error al actualizar Factura en la base de datos: {ex}", EventLevel.Error);
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
