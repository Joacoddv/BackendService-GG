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
    class MenuRepository : IGenericRepository<Menu>
    {


        #region Statements
        private string InsertStatement
        {
            get => "INSERT INTO [dbo].[MENU] (Id_Empresa,Id_Sucursal,Id_Menu,Fecha_Alta_Menu,Fecha_Dia_Menu,Id_Plato,Estado,Precio_Menu_Plato,Observaciones) VALUES (@Id_Empresa,@Id_Sucursal,@Id_Menu,@Fecha_Alta_Menu,@Fecha_Dia_Menu,@Id_Plato,@Estado,@Precio_Menu_Plato,@Observaciones)";
        }

        private string UpdateStatement
        {
            get => "UPDATE [dbo].[MENU] SET Id_Menu=@Id_Menu,Fecha_Dia_Menu=@Fecha_Dia_Menu,Id_Plato=@Id_Plato,Estado=@Estado,Precio_Menu_Plato=@Precio_Menu_Plato,Observaciones=@Observaciones WHERE  Id_Menu= @Id_Menu and Id_Empresa=@Id_Empresa and Id_Sucursal=@Id_Sucursal";
        }

        private string UpdateStatementEstado
        {
            get => "UPDATE [dbo].[MENU] SET Estado=@Estado WHERE  Id_Menu= @Id_Menu and Id_Empresa=@Id_Empresa and Id_Sucursal=@Id_Sucursal";
        }

        private string DeleteStatement
        {
            get => "DELETE FROM [dbo].[MENU] WHERE Id_Menu = @Id_Menu and Id_Empresa=@Id_Empresa and Id_Sucursal=@Id_Sucursal";
        }

        private string SelectOneStatement
        {
            get => "SELECT Id_Empresa,Id_Sucursal,Id_Menu,Numero_Menu,Fecha_Alta_Menu,Fecha_Dia_Menu,Id_Plato,Estado,Precio_Menu_Plato,Observaciones FROM [dbo].[MENU] WHERE  Id_Menu= @Id_Menu and Id_Empresa=@Id_Empresa and Id_Sucursal=@Id_Sucursal";
        }

        private string SelectAllStatement
        {
            get => "SELECT Id_Empresa,Id_Sucursal,Id_Menu,Numero_Menu,Fecha_Alta_Menu,Fecha_Dia_Menu,Id_Plato,Estado,Precio_Menu_Plato,Observaciones FROM [dbo].[MENU] where Id_Empresa=@Id_Empresa and Id_Sucursal=@Id_Sucursal";
        }
        #endregion

        public void Delete(Menu obj)
        {
            try
            {
                LoggerManager.Current.Write("DAL Menu - Eliminando Menu en la Base de Datos", EventLevel.Informational);
                int y = SqlHelper.ExecuteNonQuery(UpdateStatementEstado, System.Data.CommandType.Text,
                                                   new SqlParameter[] {
                                                   new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                                   new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                                   new SqlParameter("@Id_Menu", Guid.Parse(obj.Id_Menu.ToString())),
                                                   new SqlParameter("@Estado", false)});
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Menu - Error al borrar menu de la base de datos: {ex}", EventLevel.Error);
            }
        }

        public IEnumerable<Menu> GetAll(Menu obj)
        {
            List<Menu> menus = new List<Menu>();
            try
            {
                LoggerManager.Current.Write("DAL Menu - Buscando Menus en la Base de Datos", EventLevel.Informational);

                using (var dr = SqlHelper.ExecuteReader(SelectAllStatement, System.Data.CommandType.Text,
                                new SqlParameter[] {
                                new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString()))}))
                {
                    while (dr.Read())
                    {
                        Menu menu = new Menu();

                        object[] values = new object[dr.FieldCount];
                        dr.GetValues(values);

                        menu = MenuAdapter.Current.Adapt(values);

                        menus.Add(menu);
                    }


                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Menu - Error al buscar menus de la base de datos: {ex}", EventLevel.Error);
            }
            return menus;
        }

        public Menu GetOne(Menu obj)
        {
            Menu menu = new Menu();

            LoggerManager.Current.Write("DAL Menu - Buscando un Menu en la Base de Datos", EventLevel.Informational);

            try
            {
                using (var dr = SqlHelper.ExecuteReader(SelectOneStatement, System.Data.CommandType.Text,
                                                        new SqlParameter[] {
                                                        new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                                        new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                                        new SqlParameter("@Id_Menu", Guid.Parse(obj.Id_Menu.ToString())) }))
                {
                    if (dr.Read())
                    {
                        //En este caso tendremos un solo registro...
                        object[] values = new object[dr.FieldCount];
                        dr.GetValues(values);

                        menu = MenuAdapter.Current.Adapt(values);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Menu - Error al buscar un menu de la base de datos: {ex}", EventLevel.Error);
            }
            return menu;
        }

        public void Insert(Menu obj)
        {
            try
            {
                LoggerManager.Current.Write("DAL Menu - Insertando Menu en la Base de Datos", EventLevel.Informational);
                int x = SqlHelper.ExecuteNonQuery(InsertStatement, System.Data.CommandType.Text,
                                                                       new SqlParameter[] {
                                              new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                              new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                              new SqlParameter("@Id_Menu", Guid.Parse(obj.Id_Menu.ToString())),
                                              //new SqlParameter("@Numero_Menu", obj.Numero_Menu),
                                              new SqlParameter("@Fecha_Alta_Menu", obj.Fecha_Alta_Menu),
                                              new SqlParameter("@Fecha_Dia_Menu", obj.Fecha_Dia_Menu),
                                              new SqlParameter("@Id_Plato", Guid.Parse(obj.Plato.Id_Plato.ToString())),
                                              new SqlParameter("@Estado", obj.Estado),
                                              new SqlParameter("@Precio_Menu_Plato", obj.Precio_Menu_Plato),
                                              new SqlParameter("@Observaciones", obj.Observaciones)});
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Menu - Error al insertar menu de la base de datos: {ex}", EventLevel.Error);
            }
        }

        public void Update(Menu obj)
        {
            try
            {
                LoggerManager.Current.Write("DAL Menu - Actualizando Menu en la Base de Datos", EventLevel.Informational);
                int x = SqlHelper.ExecuteNonQuery(UpdateStatement, System.Data.CommandType.Text,
                                                                       new SqlParameter[] {
                                              new SqlParameter("@Id_Empresa", Guid.Parse(obj.Id_Empresa.ToString())),
                                              new SqlParameter("@Id_Sucursal", Guid.Parse(obj.Id_Sucursal.ToString())),
                                              new SqlParameter("@Id_Menu", Guid.Parse(obj.Id_Menu.ToString())),
                                              //new SqlParameter("@Numero_Menu", obj.Numero_Menu),
                                              //new SqlParameter("@Fecha_Alta_Menu", obj.Fecha_Alta_Menu),
                                              new SqlParameter("@Fecha_Dia_Menu", obj.Fecha_Dia_Menu),
                                              new SqlParameter("@Id_Plato", Guid.Parse(obj.Plato.Id_Plato.ToString())),
                                              new SqlParameter("@Estado", obj.Estado),
                                              new SqlParameter("@Precio_Menu_Plato", obj.Precio_Menu_Plato),
                                              new SqlParameter("@Observaciones", obj.Observaciones)});

            }

            catch (Exception ex)
            {
                LoggerManager.Current.Write($"DAL Menu - Error al actualizar menu de la base de datos: {ex}", EventLevel.Error);
            }
        }
    }
}
