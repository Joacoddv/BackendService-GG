using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Servicios.DAL.Tools;
using Servicios.Domain.Usuario_Patente_Familia;

namespace Servicios.DAL.Usuario_Patente_Familia
{


    internal sealed class DALUsuario
    {
        private readonly static DALUsuario _instance = new DALUsuario();

        public static DALUsuario Current
        {
            get
            {
                return _instance;
            }
        }

        private DALUsuario()
        {
            //Implent here the initialization of your singleton
        }

        #region Statements
        private static string SelectAll
        {
            get => "SELECT IdUsuario,Numero_Usuario, Mail, Nombre, Apellido,Fecha_Alta, PasswordHash,PasswordSalt, Estado,Idioma FROM [dbo].[Usuario]";
        }

        private static string SelectOne
        {
            get => "SELECT IdUsuario,Numero_Usuario, Mail, Nombre, Apellido,Fecha_Alta, PasswordHash,PasswordSalt, Estado,Idioma FROM [dbo].[Usuario] WHERE Mail = @Mail";
        }


        private static string InsertUser
        {
            get => "INSERT INTO [dbo].[Usuario] ([IdUsuario],[Mail],[Nombre],[Apellido],[Fecha_Alta],[PasswordHash],[PasswordSalt],[Estado],[Idioma]) " +
                "VALUES (@IdUsuario, @Mail, @Nombre, @Apellido,@Fecha_Alta, @PasswordHash,@PasswordSalt, @Estado,@Idioma)";
        }
        private static string SelectCountStatement
        {
            get => "SELECT COUNT(IdUsuario) FROM [dbo].[Usuario] where " +
                "Mail = @Mail AND PasswordHash = @PasswordHash AND PasswordSalt = @PasswordSalt";
        }
        private string UpdateStatement
        {
            get => "UPDATE [dbo].[Usuario] SET Estado=@Estado, Idioma=@Idioma WHERE IdUsuario = @IdUsuario";
        }
        #endregion

        public static bool BuscarUsuario(Usuario usuario)
        {

            using (SqlConnection sqlConn = new SqlConnection(ConfigurationManager.ConnectionStrings["SecConString"].ConnectionString))
            {
                try
                {
                    sqlConn.Open();

                    using (SqlCommand cmd = new SqlCommand(SelectCountStatement, sqlConn))
                    {
                        cmd.CommandType = CommandType.Text;

                        cmd.Parameters.AddWithValue("Mail", usuario.Mail);
                        cmd.Parameters.AddWithValue("PasswordHash", usuario.PasswordHash);
                        cmd.Parameters.AddWithValue("PasswordSalt", usuario.PasswordSalt);

                        int count = Convert.ToInt32(cmd.ExecuteScalar());

                        if (count == 0)
                            return false;

                        else
                            return true;
                    }

                }
                catch (Exception ex)
                {

                    throw ex;
                }
                finally
                {
                    sqlConn.Close();
                }
            }
        }
        public IEnumerable<Usuario> GetAll()
        {
            try
            {
                List<Usuario> usuarios = new List<Usuario>();

                using (var dr = SqlHelper.ExecuteReader(SelectAll, CommandType.Text))
                {
                    Object[] values = new Object[dr.FieldCount];

                    while (dr.Read())
                    {
                        dr.GetValues(values);

                        Usuario usuario = new Usuario();
                        usuario.UserId = Guid.Parse(values[0].ToString());
                        usuario.Numero_Usuario = Convert.ToInt32(values[1]);
                        usuario.Mail = values[2].ToString();
                        usuario.Nombre = values[3].ToString();
                        usuario.Apellido = values[4].ToString();
                        usuario.Fecha_Alta = (DateTime)(!string.IsNullOrEmpty(values[5]?.ToString()) ? Convert.ToDateTime(values[5]) : (DateTime?)null);
                        usuario.PasswordHash = ((byte[])values[6]);
                        usuario.PasswordSalt = ((byte[])values[7]);
                        usuario.Estado = Convert.ToBoolean(values[8]);
                        usuario.Idioma = values[9].ToString();

                        usuarios.Add(usuario);
                    }
                }
                return usuarios;
            }
            catch (Exception ex)
            {

                return null;
            }
        }

        public Usuario GetOneValidate(Usuario usuario)
        {
            try
            {
                string sqlStatement = SelectOne;

                List<SqlParameter> p = new List<SqlParameter>();

                    p.Add(new SqlParameter("@Mail", usuario.Mail));
                

                using (var dr = SqlHelper.ExecuteReader(sqlStatement, CommandType.Text, p.ToArray()))
                {
                    Object[] values = new Object[dr.FieldCount];

                    Usuario usuariobdd = new Usuario();

                    while (dr.Read())
                    {
                        dr.GetValues(values);

                        usuariobdd.UserId = Guid.Parse(values[0].ToString());
                        usuariobdd.Numero_Usuario = Convert.ToInt32(values[1]);
                        usuariobdd.Mail = values[2].ToString();
                        usuariobdd.Nombre = values[3].ToString();
                        usuariobdd.Apellido = values[4].ToString();
                        usuariobdd.Fecha_Alta = (DateTime)(!string.IsNullOrEmpty(values[5]?.ToString()) ? Convert.ToDateTime(values[5]) : (DateTime?)null);
                        usuariobdd.PasswordHash = System.Text.Encoding.UTF8.GetBytes(values[6].ToString());
                        usuariobdd.PasswordSalt = System.Text.Encoding.UTF8.GetBytes(values[7].ToString());
                        usuariobdd.Estado = Convert.ToBoolean(values[8]);
                        usuariobdd.Idioma = values[9].ToString();

                    }
                    return usuariobdd;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public Usuario GetOne(Usuario usuario)
        {
            try
            {
                string sqlStatement = SelectOne;

                List<SqlParameter> p = new List<SqlParameter>();
                if (usuario.PasswordHash == null)
                {
                    p.Add(new SqlParameter("@Mail", usuario.Mail));
                }
                else
                {
                    p.Add(new SqlParameter("@Mail", usuario.Mail));
                    p.Add(new SqlParameter("@PasswordHash", usuario.PasswordHash));
                    p.Add(new SqlParameter("@PasswordSalt", usuario.PasswordSalt));

                    sqlStatement += " and Password = @Password and Estado = '1'";
                }

                using (var dr = SqlHelper.ExecuteReader(sqlStatement, CommandType.Text, p.ToArray()))
                {
                    Object[] values = new Object[dr.FieldCount];

                    Usuario usuariobdd = new Usuario();

                    while (dr.Read())
                    {
                        dr.GetValues(values);

                        usuariobdd.UserId = Guid.Parse(values[0].ToString());
                        usuariobdd.Numero_Usuario = Convert.ToInt32(values[1]);
                        usuariobdd.Mail = values[2].ToString();
                        usuariobdd.Nombre = values[3].ToString();
                        usuariobdd.Apellido = values[4].ToString();
                        usuariobdd.Fecha_Alta = (DateTime)(!string.IsNullOrEmpty(values[5]?.ToString()) ? Convert.ToDateTime(values[5]) : (DateTime?)null);
                        usuariobdd.PasswordHash = ((byte[])values[6]);
                        usuariobdd.PasswordSalt = ((byte[])values[7]);
                        usuariobdd.Estado = Convert.ToBoolean(values[8]);
                        usuariobdd.Idioma = values[9].ToString();


                    }
                    return usuariobdd;
                }
            }
            catch (Exception ex)
            {
                return null;
            }
        }


        public void AddUser(Usuario usuario)
        {
            //    try
            //    {
            List<SqlParameter> p = new List<SqlParameter>();

            p.Add(new SqlParameter("@IdUsuario", usuario.UserId));
            p.Add(new SqlParameter("@Mail", usuario.Mail));
            p.Add(new SqlParameter("@Nombre", usuario.Nombre));
            p.Add(new SqlParameter("@Apellido", usuario.Apellido));
            p.Add(new SqlParameter("@Fecha_Alta", DateTime.Now));
            p.Add(new SqlParameter("@PasswordHash", usuario.PasswordHash));
            p.Add(new SqlParameter("@PasswordSalt", usuario.PasswordSalt));
            p.Add(new SqlParameter("@Estado", usuario.Estado));
            p.Add(new SqlParameter("@Idioma", usuario.Idioma));

            SqlHelper.ExecuteNonQuery(InsertUser, CommandType.Text, p.ToArray());

            //}
            //catch (Exception ex)
            //{
            //}
        }


        public void UpdateUser(Usuario usuario)
        {
            //    try
            //    {
            List<SqlParameter> p = new List<SqlParameter>();

            p.Add(new SqlParameter("@IdUsuario", usuario.UserId));
            p.Add(new SqlParameter("@Mail", usuario.Mail));
            p.Add(new SqlParameter("@Nombre", usuario.Nombre));
            p.Add(new SqlParameter("@Apellido", usuario.Apellido));
            p.Add(new SqlParameter("@Fecha_Alta", DateTime.Now));
            p.Add(new SqlParameter("@PasswordHash", usuario.PasswordHash));
            p.Add(new SqlParameter("@PasswordSalt", usuario.PasswordSalt));
            p.Add(new SqlParameter("@Estado", usuario.Estado));
            p.Add(new SqlParameter("@Idioma", usuario.Idioma));

            SqlHelper.ExecuteNonQuery(UpdateStatement, CommandType.Text, p.ToArray());

            //}
            //catch (Exception ex)
            //{
            //}
        }
    }
}
