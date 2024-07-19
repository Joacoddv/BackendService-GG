using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics.Tracing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Servicios.DAL.Usuario_Patente_Familia;
using Servicios.Domain.Usuario_Patente_Familia;
using Servicios.Services;
using Servicios.Services.Extensions;

namespace Servicios.BLL.Usuario_Patente_Familia
{
    public sealed class BLLUsuario
    {
        private readonly static BLLUsuario _instance = new BLLUsuario();

        public static BLLUsuario Current
        {
            get
            {
                return _instance;
            }
        }

        private BLLUsuario()
        {
            //Implent here the initialization of your singleton
        }




        public static void ActualizarIdioma(Usuario usuario)
        {
            try
            {
                DALUsuario.Current.UpdateUser(usuario);
                LoggerManager.Current.Write($"BLL Usuario - Actualizando idioma del usuario {usuario.Mail}", EventLevel.Informational);
                ConfigurationManager.AppSettings["Idioma"] = usuario.Idioma.ToString();
            }

            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Usuario - Error al actualizar idioma del usuario {usuario.Mail}", EventLevel.Informational);
                throw new Exception("Error al actualizar idioma del sistema:" + $"{ex.Message}");
            }
        }



        public static void ActualizarEstado(Usuario usuario)
        {
            try
            {
                if (usuario.Estado == true)
                {
                    usuario.Estado = false;
                }
                else if (usuario.Estado == false)
                {
                    usuario.Estado = true;
                }
                DALUsuario.Current.UpdateUser(usuario);
                LoggerManager.Current.Write($"BLL Usuario - Actualizando estado del usuario {usuario.Mail}", EventLevel.Informational);
            }

            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Usuario - Eror al actualizar estado del usuario {usuario.Mail}", EventLevel.Informational);
                throw new Exception($"Eror al actualizar estado del usuario:" + $"{ex.Message}");
            }
        }


        public static bool BuscarUsuario(Usuario usuario)
        {
            try
            {
                if (DALUsuario.BuscarUsuario(usuario))
                {
                    if ((usuario = DALUsuario.Current.GetOne(usuario)).Estado == true)
                    {
                        LoggerManager.Current.Write("BLL Usuario - Inicio de Sesion Exitoso", EventLevel.Informational);
                        return true;
                    }
                    return false;
                }

                else
                {
                    LoggerManager.Current.Write($"BLL Usuario - Inicio de Sesion Fallido {usuario.Mail}", EventLevel.Informational);
                    throw new Exception("Usuario o Contraseña incorrecta");
                }

            }

            catch (Exception ex)
            {
                throw new Exception("Usuario o Contraseña incorrecta");
            }
        }

        public static IEnumerable<Usuario> GetAll()
        {
            try
            {
                return DALUsuario.Current.GetAll();
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static Usuario GetOne(Usuario usuario)
        {
            try
            {
                return DALUsuario.Current.GetOne(usuario);
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        //public static Usuario DesactivarUsuario(string userName)
        //{
        //    try
        //    {
        //        DALUsuario.Current.
        //    }
        //    catch (Exception ex)
        //    {
        //        return null;
        //    }
        //}

        public static void CrearUsuario(Usuario usuario)
        {
            try
            {
                if (DALUsuario.Current.GetOneValidate(usuario).Mail == null)
                {
                    DALUsuario.Current.AddUser(usuario);
                    LoggerManager.Current.Write($"BLL Usuario - Creando usuario {usuario.Mail}", EventLevel.Informational);
                }
                else
                {
                    throw new Exception("Ya esxiste el usuario ".Traducir() + $" {usuario.Mail}");
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Usuario - Error al crear usuario {usuario.Mail}", EventLevel.Informational);
                throw new Exception($"{ex.Message}");
            }
        }
        public static IEnumerable<Patente> GetPatentesAsignadas(Usuario usuario)
        {
            try
            {
                return DALUsuario_Patente.Current.GetPatentesAsignadas(usuario);
            }
            catch (Exception ex)
            {
                return null;
            }
        }






        private void CargarPermisos(Usuario usuario)
        {
            try
            {
                LoggerManager.Current.Write($"BLL Usuario - Cargando permisos del usuario", EventLevel.Informational);
                usuario.Permisos.Clear();
                foreach (var item in DALUsuario_Familia.Current.GetFamiliasAsignadas(usuario))
                {
                    //usuario.Permisos.Add(item);
                    //foreach (var itempatente in BLLFamilia.Current.GetPatentesAsignadasaFamilia(item))
                    //{
                    //    item.Agregar(itempatente);
                    //}
                    //foreach (var itemfamilia in BLLFamilia.Current.GetFamiliasAsignadas(item))
                    //{
                    //    item.Agregar(itemfamilia);
                    //}
                    //BLLFamilia.Current.GetPatentesAsignadasaFamilia(item, usuario);

                    BLLFamilia.Current.HidratarFamilia(item);
                    if (usuario.Permisos.Any(o => o.Nombre.Equals(item.Nombre)))
                    {

                    }
                    else
                    {
                        usuario.Permisos.Add(item);
                    }


                }

                foreach (var item in DALUsuario_Patente.Current.GetPatentesAsignadas(usuario))
                {
                    if (usuario.Permisos.Any(o => o.Nombre.Equals(item.Nombre)))
                    {

                    }
                    else
                    {
                        usuario.Permisos.Add(item);
                    }

                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Usuario - Error al cargar permisos del usuario {usuario.Mail}", EventLevel.Error);
                throw new Exception("Error al cargar permisos del usuario".Traducir() + $"{usuario.Mail}" + $": {ex.Message}");
            }

        }

        private void ObtenerListadoPatentes(List<PatenteFamilia> lstPermisosPorGrupo, List<Patente> listadoAux)
        {
            try
            {
                LoggerManager.Current.Write($"BLL Usuario - Obteniendo listado de pantentes", EventLevel.Informational);
                foreach (var permisoItem in lstPermisosPorGrupo)
                {
                    if (permisoItem.CantidadHijos == 0)
                    {
                        //Console.WriteLine("Patente: " + permisoItem.Nombre);
                        if (listadoAux.Any(o => o.Nombre.Equals(permisoItem.Nombre)))
                        {

                        }
                        else
                        {
                            listadoAux.Add(permisoItem as Patente);
                        }

                    }
                    if (permisoItem.CantidadHijos > 0)
                    {
                        ObtenerListadoPatentes((permisoItem as Familia).ListadoHijos, listadoAux);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Usuario - Error al obtener listado de patentes", EventLevel.Informational);
                throw new Exception("Error al obtener listado de patentes".Traducir() + $": {ex.Message}");
            }
        }

        private List<Patente> PermisosMenu(Usuario usuario)
        {
            try
            {
                List<Patente> listadoAux = new List<Patente>();
                CargarPermisos(usuario);
                ObtenerListadoPatentes(usuario.Permisos, listadoAux);
                foreach (var item in listadoAux)
                {
                    if (item != null)
                    {
                        item.Nombre = DALPatente.Current.GetOne(item).Nombre;
                    }
                }
                return listadoAux;
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Usuario - Error al obtener permisos Menu", EventLevel.Informational);
                throw new Exception($"Error: {ex.Message}");
            }
        }




        public bool ValidarPermisoparaForm(Usuario usuario, Form form)
        {
            try
            {
                LoggerManager.Current.Write($"BLL Usuario - Validando permisos para el formulario", EventLevel.Informational);
                bool estado = false;
                if (PermisosMenu(usuario).Any(o => o.Nombre.Equals(form.Name)))
                {
                    estado = true;
                }
                return estado;
            }
            catch (Exception ex)
            {
                LoggerManager.Current.Write($"BLL Usuario - Error al validar permisos para el formulario", EventLevel.Informational);
                throw new Exception($"Error: {ex.Message}");
            }
        }

        public static IEnumerable<Patente> GetPatentesDisponibles(Usuario usuario)
        {
            try
            {
                return DALUsuario_Patente.Current.GetPatentesDisponibles(usuario);
            }
            catch (Exception ex)
            {
                return null;
            }
        }
        public static void AddUsuarioPatente(Usuario usuario, Patente patente)
        {
            try
            {
                DALUsuario_Patente.Current.AddUsuarioPatente(usuario, patente);

                //Registro en el Log
                LoggerManager.Current.Write("Agrego patente a usuario", EventLevel.Informational);
            }
            catch (Exception ex)
            {

            }
        }

        public static void DeleteUsuarioPatente(Usuario usuario, Patente patente)
        {
            try
            {
                DALUsuario_Patente.Current.DeleteUsuarioPatente(usuario, patente);

                LoggerManager.Current.Write("Elimino Patente de Usuario", EventLevel.Informational);
            }
            catch (Exception ex)
            {

            }
        }

        public static IEnumerable<Familia> GetFamiliasAsignadas(Usuario usuario)
        {
            try
            {
                return DALUsuario_Familia.Current.GetFamiliasAsignadas(usuario);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static IEnumerable<Familia> GetFamiliasDisponibles(Usuario usuario)
        {
            try
            {
                return DALUsuario_Familia.Current.GetFamiliasDisponibles(usuario);
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public static void AddUsuarioFamilia(Usuario usuario, Familia familia)
        {
            try
            {
                DALUsuario_Familia.Current.AddUsuarioFamilia(usuario, familia);

                //Registro en el Log
                LoggerManager.Current.Write("Agrego Usuario a Familia", EventLevel.Informational);
            }
            catch (Exception ex)
            {

            }
        }

        public static void DeleteUsuarioFamilia(Usuario usuario, Familia familia)
        {
            try
            {
                DALUsuario_Familia.Current.DeleteUsuarioFamilia(usuario, familia);

                //Registro en el Log
                LoggerManager.Current.Write("Elimino Usuario de familia", EventLevel.Informational);
            }
            catch (Exception ex)
            {

            }
        }
    }
}
