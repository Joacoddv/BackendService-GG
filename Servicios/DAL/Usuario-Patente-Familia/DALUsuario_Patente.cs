using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Servicios.DAL.Tools;
using Servicios.Domain.Usuario_Patente_Familia;

namespace Servicios.DAL.Usuario_Patente_Familia
{

    internal sealed class DALUsuario_Patente
    {
        private readonly static DALUsuario_Patente _instance = new DALUsuario_Patente();

        public static DALUsuario_Patente Current
        {
            get
            {
                return _instance;
            }
        }

        private DALUsuario_Patente()
        {
            //Implent here the initialization of your singleton
        }

        #region Statements
        private static string SelectAsignadas
        {
            get => "SELECT A.IdPatente, B.Nombre " +
                "FROM [dbo].[Usuario_Patente] A " +
                "INNER JOIN [dbo].[Patente] B on B.IdPatente = A.IdPatente " +
                "WHERE A.IdUsuario = @IdUsuario";
        }
        private static string SelectDisponibles
        {
            get => "SELECT A.IdPatente, A.Nombre " +
                "FROM [dbo].[Patente] A " +
                "WHERE A.IdPatente not in (SELECT B.IdPatente FROM Usuario_Patente B WHERE B.IdUsuario = @IdUsuario)";
        }
        private static string Insert
        {
            get => "INSERT INTO [dbo].[Usuario_Patente] ([IdUsuario],[IdPatente]) VALUES (@IdUsuario, @IdPatente)";
        }
        private static string Delete
        {
            get => "DELETE [dbo].[Usuario_Patente] WHERE IdUsuario = @IdUsuario AND IdPatente = @IdPatente";
        }
        #endregion


        public IEnumerable<Patente> GetPatentesAsignadas(Usuario usuario)
        {
            try
            {
                List<SqlParameter> p = new List<SqlParameter>();
                p.Add(new SqlParameter("@IdUsuario", usuario.UserId));

                List<Patente> patentes = new List<Patente>();

                using (var dr = SqlHelper.ExecuteReader(SelectAsignadas, CommandType.Text, p.ToArray()))
                {
                    Object[] values = new Object[dr.FieldCount];

                    while (dr.Read())
                    {
                        dr.GetValues(values);

                        Patente patente = new Patente();
                        patente.IdPatente = Guid.Parse(values[0].ToString());
                        patente.Nombre = values[1].ToString();

                        patentes.Add(patente);
                    }
                }
                return patentes;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public IEnumerable<Patente> GetPatentesDisponibles(Usuario usuario)
        {
            try
            {
                List<SqlParameter> p = new List<SqlParameter>();
                p.Add(new SqlParameter("@IdUsuario", usuario.UserId));

                List<Patente> patentes = new List<Patente>();

                using (var dr = SqlHelper.ExecuteReader(SelectDisponibles, CommandType.Text, p.ToArray()))
                {
                    Object[] values = new Object[dr.FieldCount];

                    while (dr.Read())
                    {
                        dr.GetValues(values);

                        Patente patente = new Patente();
                        patente.IdPatente = Guid.Parse(values[0].ToString());
                        patente.Nombre = values[1].ToString();

                        patentes.Add(patente);
                    }
                }
                return patentes;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public void AddUsuarioPatente(Usuario usuario, Patente patente)
        {
            try
            {
                List<SqlParameter> p = new List<SqlParameter>();

                p.Add(new SqlParameter("@IdUsuario", usuario.UserId));
                p.Add(new SqlParameter("@IdPatente", patente.IdPatente));

                SqlHelper.ExecuteNonQuery(Insert, CommandType.Text, p.ToArray());

            }
            catch (Exception ex)
            {
            }
        }

        public void DeleteUsuarioPatente(Usuario usuario, Patente patente)
        {
            try
            {
                List<SqlParameter> p = new List<SqlParameter>();

                p.Add(new SqlParameter("@IdUsuario", usuario.UserId));
                p.Add(new SqlParameter("@IdPatente", patente.IdPatente));

                SqlHelper.ExecuteNonQuery(Delete, CommandType.Text, p.ToArray());
            }
            catch (Exception ex)
            {
            }
        }
    }
}
